using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using HomeNetOrm.Repositories.Base;
using System.Data.Common;

namespace HomeNetOrm.DBProviders.Postgres
{
    internal static class SchemaPostgresTriggersRepoExtensions
    {
        /// <summary>
        /// Автоматический накат триггеров версионирования и триггеров надгробий (Tombstones) для PostgreSQL.
        /// </summary>
        public static async Task MigratePostgresVersionTriggersAsync(
            this SchemaRepository _,
            DbConnection connection,
            ISchemaSqlGenerator schemaSqlInitializer,
            ISqlDialectTranslator translator,
            IEnumerable<TableSchema> tableSchemas,
            ICastomLogger logger)
        {
            logger.LogInfo("[Postgres Migrator] Серверный режим: Проверка триггеров версионирования и надгробий...");

            try
            {
                // 1. Создаем/обновляем глобальную функцию инкремента версий
                string functionSql = schemaSqlInitializer.GenerateVersionFunctionSql(translator);
                await ExecuteSqlAsync(connection, functionSql);
                logger.LogInfo("[Postgres Migrator] ✅ Глобальная функция 'update_version_func' проверена.");

                // 2. Адаптируем схему DeletedRecords в snake_case через твой турникет
                IEnumerable<TableSchema> tableSchemata = tableSchemas as TableSchema[] ?? [.. tableSchemas];
                TableSchema rawDeletedSchema = tableSchemata.First(s =>
                    s.TableName.Replace("\"", "").Equals("DeletedRecords", StringComparison.OrdinalIgnoreCase));
                TableSchema snakeDeletedSchema = rawDeletedSchema.ToAdaptedSchemas().Snake;

                string deletedTable = translator.FormatTable(snakeDeletedSchema);
                string colId = translator.FormatColumnName("id");
                string colEntityType = translator.FormatColumnName("entity_type");
                string colDeletedAt = translator.FormatColumnName("deleted_at");

                // 3. Создаем/обновляем универсальную функцию записи в таблицу надгробий
                string tombstoneFuncSql = $@"
                    CREATE OR REPLACE FUNCTION log_deleted_record()
                    RETURNS TRIGGER AS $$
                    BEGIN
                        INSERT INTO {deletedTable} ({colId}, {colEntityType}, {colDeletedAt})
                        VALUES (OLD.{colId}, TG_ARGV[0], NOW())
                        ON CONFLICT ({colId}) DO UPDATE SET {colDeletedAt} = NOW();
                        RETURN OLD;
                    END;
                    $$ LANGUAGE plpgsql;";
                await ExecuteSqlAsync(connection, tombstoneFuncSql);
                logger.LogInfo("[Postgres Migrator] ✅ Функция надгробий 'log_deleted_record' проверена.");

                // 4. Бежим по всем схемам таблиц
                foreach (TableSchema schema in tableSchemata)
                {
                    if (string.IsNullOrEmpty(schema.TableName)) continue;

                    // Проверяем по самой схеме: есть ли у таблицы отслеживание версий (колонка Version)
                    bool hasVersionColumn = schema.Columns.Any(c =>
                        c.Name.Equals("Version", StringComparison.OrdinalIgnoreCase));

                    if (!hasVersionColumn)
                    {
                        logger.LogInfo($"[Postgres Migrator] Таблица '{schema.TableName}' не использует версионирование. Пропуск.");
                        continue;
                    }

                    string cleanTableName = schema.TableName.Replace("\"", "").ToLower();

                    // --- ШАГ А: Триггер версионирования (BEFORE INSERT/UPDATE) ---
                    string versionTriggerName = $"trg_{cleanTableName}_version";
                    if (!await TriggerExistsAsync(connection, versionTriggerName))
                    {
                        logger.LogWarning($"[Postgres Migrator] Триггер '{versionTriggerName}' не найден. Накатываем...");
                        string triggerSql = schemaSqlInitializer.GenerateVersionTriggerSql(schema, translator);
                        await ExecuteSqlAsync(connection, triggerSql);
                        logger.LogInfo($"[Postgres Migrator] ✅ Триггер '{versionTriggerName}' привязан к '{schema.TableName}'.");
                    }

                    // --- ШАГ Б: Триггер надгробий (AFTER DELETE) для всех версионируемых таблиц, кроме самой DeletedRecords ---
                    if (cleanTableName.Equals("deletedrecords", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string deleteTriggerName = $"trg_{cleanTableName}_deleted";
                    if (await TriggerExistsAsync(connection, deleteTriggerName))
                    {
                        continue;
                    }

                    logger.LogWarning($"[Postgres Migrator] Триггер надгробий '{deleteTriggerName}' не найден. Накатываем...");

                    TableSchema snakeSchema = schema.ToAdaptedSchemas().Snake;
                    string quotedTable = translator.FormatTable(snakeSchema);
                    string entityTypeArg = schema.TableName.Replace("\"", "");

                    string deleteTriggerSql = $@"
                                CREATE TRIGGER {deleteTriggerName}
                                AFTER DELETE ON {quotedTable}
                                FOR EACH ROW EXECUTE FUNCTION log_deleted_record('{entityTypeArg}');";

                    await ExecuteSqlAsync(connection, deleteTriggerSql);
                    logger.LogInfo($"[Postgres Migrator] ✅ Триггер надгробий '{deleteTriggerName}' привязан к '{schema.TableName}'.");
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical($"[Postgres Migrator] ❌ КРИТИЧЕСКАЯ ОШИБКА инициализации триггеров: {ex.Message}");
                throw;
            }
        }

        private static async Task<bool> TriggerExistsAsync(DbConnection connection, string triggerName)
        {
            await using DbCommand checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM pg_trigger WHERE tgname = @tgname;";
            DbParameter param = checkCmd.CreateParameter();
            param.ParameterName = "@tgname";
            param.Value = triggerName;
            checkCmd.Parameters.Add(param);

            object? result = await checkCmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        private static async Task ExecuteSqlAsync(DbConnection connection, string sql)
        {
            await using DbCommand cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
