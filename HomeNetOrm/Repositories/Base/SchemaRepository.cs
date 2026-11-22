using Dapper;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using System.Data;
using System.Data.Common;

namespace HomeNetOrm.Repositories.Base
{
    internal class SchemaRepository
    {
        private readonly DbConnection _dbConnection;
        private readonly ISchemaSqlGenerator _schemaSqlInitializer;
        private readonly ISchemaProvider _schemaProvider;
        private readonly IEnumerable<TableSchema> _tableSchemas;
        private readonly ICastomLogger _logger;
        private readonly string _dbName;

        public SchemaRepository(
            DbConnection connection,
            ISchemaProvider schemaProvider,
            ISchemaSqlGenerator schemaSqlInitializer,
            IEnumerable<TableSchema> tableSchemas,
            ICastomLogger logger)
        {
            _dbConnection = connection ?? throw new ArgumentNullException(nameof(connection));
            _schemaProvider = schemaProvider ?? throw new ArgumentNullException(nameof(schemaProvider));
            _schemaSqlInitializer = schemaSqlInitializer ?? throw new ArgumentNullException(nameof(schemaSqlInitializer));
            _tableSchemas = tableSchemas ?? throw new ArgumentNullException(nameof(tableSchemas));
            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));

            _dbName = _dbConnection.GetType().Name.Contains("NpgsqlConnection") ? "PostgresSQL" : "SqLite";
        }

        public async Task InitializeAsync()
        {
            _logger.LogInfo($"=== 🚀 СТАРТ ИНИЦИАЛИЗАЦИИ БАЗЫ ДАННЫХ [{_dbName}] ===");

            // 🌟 LINQ-фильтрация: Убираем из цепочки весь мусор, null-объекты и пустые схемы одной декларативной строкой
            IEnumerable<(TableSchema Raw, AdaptedSchemas Adapted)> validSchemas = _tableSchemas
                .Where(s => !string.IsNullOrEmpty(s.TableName))
                .Select(s => (Raw: s, Adapted: s.ToAdaptedSchemas()))
                .Where(x => x.Adapted?.Snake?.TableName != null && x.Adapted?.Camel?.TableName != null);

            foreach ((TableSchema raw, AdaptedSchemas adapted) in validSchemas)
            {
                string camelName = adapted.Camel.TableName;
                TableSchema snakeSchema = adapted.Snake;

                try
                {
                    _logger.LogInfo($"[{_dbName}] 🔍 Проверка таблицы: {camelName}...");

                    if (!await TableExistsAsync(snakeSchema.TableName))
                    {
                        await CreateTableAsync(snakeSchema, camelName);
                    }
                    else
                    {
                        _logger.LogInfo($"[{_dbName}] Таблица {camelName} существует, сверяю структуру...");
                        await CheckTableStructureAsync(snakeSchema, camelName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[{_dbName} ❌ КРИТ] Сбой при обработке таблицы {camelName}: {ex.Message}");
                    throw;
                }
            }

            _logger.LogInfo($"=== ✨ ИНИЦИАЛИЗАЦИЯ БАЗЫ ДАННЫХ [{_dbName}] ЗАВЕРШЕНА ===");
        }

        private async Task<bool> TableExistsAsync(string dbTableName)
        {
            SqlQueryCommand cmd = _schemaSqlInitializer.GenerateTableExistsSql(dbTableName);

            try
            {
                int result = await _dbConnection.ExecuteScalarAsync<int>(cmd.Sql, cmd.Params);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[{_dbName} ❌] Ошибка проверки существования {dbTableName}: {ex.Message}");
                return false;
            }
        }

        private async Task CreateTableAsync(TableSchema snakeSchema, string camelTableName)
        {
            if (string.IsNullOrEmpty(snakeSchema.TableName)) return;

            _logger.LogWarning($"[{_dbName} ⚠️] Таблица {camelTableName} не обнаружена. Запуск генерации...");

            if (_dbConnection.State != ConnectionState.Open) await _dbConnection.OpenAsync();

            string createTableSql = _schemaSqlInitializer.GenerateCreateTableSql(snakeSchema);
            await _dbConnection.ExecuteAsync(createTableSql);

            if (await TableExistsAsync(snakeSchema.TableName))
                _logger.LogInfo($"[{_dbName}] ✅ Таблица {camelTableName} успешно создана в БД и готова к работе.");
            else
                _logger.LogError($"[{_dbName}] ❌ Ошибка создания! Таблица {camelTableName} отсутствует после выполнения скрипта.");
        }

        private async Task CheckTableStructureAsync(TableSchema snakeSchema, string camelTableName)
        {
            if (string.IsNullOrEmpty(snakeSchema.TableName)) return;
            if (_dbConnection.State != ConnectionState.Open) await _dbConnection.OpenAsync();

            TableSchema? expectedSchemaRaw = await _schemaProvider.GetActualTableSchemaAsync(snakeSchema.TableName);

            // Guard Clause: Проверка поврежденной или пустой схемы
            if (expectedSchemaRaw?.Columns == null || expectedSchemaRaw.Columns.Count == 0 || !expectedSchemaRaw.Columns.Any(c => c.IsPrimaryKey))
            {
                _logger.LogError($"[{_dbName} ❌ ИНИЦИАЛИЗАТОР] Сверка структуры для таблицы '{camelTableName}' пропущена, так как схема в БД повреждена или пуста.");
                return;
            }

            // 1. Быстрый и чистый вызов статического компарера
            SchemaDiff? diff = SchemaComparer.Compare(expectedSchemaRaw, snakeSchema);

            if (diff.IsIdentical)
            {
                _logger.LogInfo($"[{_dbName}] ✅ Структура таблицы '{camelTableName}' в полном порядке.");
                return;
            }

            // 2. Делегируем всю черновую работу по разбору логов нашему новому визуализатору
            List<ColumnSchema>? expectedCamelCols = expectedSchemaRaw.ToAdaptedSchemas()?.Camel?.Columns;
            SchemaDiffVisualizer.LogDifferences(_logger, _dbName, camelTableName, diff, expectedCamelCols);
        }

    }
}
