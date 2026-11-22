using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNetOrm.Builders;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.Generators.Base
{
    // 🔥 Единственный базовый класс в системе! Идеален для ЛЮБОГО генератора.
    internal abstract class BaseSqlGenerator<T> : ISqlGenerator<T> where T : class
    {
        private readonly TableSchema _schema;
        private readonly TableSchema _camelSchema;
        private readonly ICastomLogger _logger;

        public ISqlDialectTranslator Translator { get; }
        private IReadOnlyDictionary<string, string> DbColumns { get; }
        public IReadOnlyDictionary<string, string> DapperParams { get; }

        protected BaseSqlGenerator(IEnumerable<TableSchema> availableSchemas, ISqlDialectTranslator translator, ICastomLogger logger)
        {
            Translator = translator ?? throw new ArgumentNullException(nameof(translator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ArgumentNullException.ThrowIfNull(availableSchemas);

            // 1. Ищем и адаптируем схему из общего реестра
            var foundSchema = FindSchemaOrThrow(availableSchemas);
            var adapted = AdaptSchemaOrThrow(foundSchema);

            _schema = adapted.Snake;
            _camelSchema = adapted.Camel;

            // 2. Вытаскиваем валидные пары колонок для маппинга
            var validPairs = ExtractValidColumnPairs();

            // 3. Формируем словари для работы с Dapper и SQL-диалектами
            DbColumns = validPairs.ToDictionary(p => p.Camel.Name!, p => p.Snake.Name!, StringComparer.OrdinalIgnoreCase);
            DapperParams = validPairs.ToDictionary(p => p.Camel.Name!, p => $"@{p.Camel.Name!}", StringComparer.OrdinalIgnoreCase);
        }

        public TokenQueryBuilder<T> CreateQuery() => new(Translator, _schema, _camelSchema, DbColumns);

        #region Private Helper Methods

        private TableSchema FindSchemaOrThrow(IEnumerable<TableSchema> availableSchemas)
        {
            var targetEntityName = typeof(T).Name;
            var cleanName = targetEntityName.Replace("Entity", "");

            var foundSchema = availableSchemas.FirstOrDefault(s =>
                !string.IsNullOrEmpty(s.TableName) &&
                (s.TableName.Equals(cleanName + "s", StringComparison.OrdinalIgnoreCase) ||
                 s.TableName.Equals(cleanName, StringComparison.OrdinalIgnoreCase)));

            if (foundSchema != null && !string.IsNullOrEmpty(foundSchema.TableName))
            {
                return foundSchema;
            }

            _logger.LogCritical($"[ОРМ ДВИЖОК] Схема для класса {targetEntityName} отсутствует в реестре.");
            throw new InvalidOperationException($"Схема для класса {targetEntityName} отсутствует.");

        }

        private (TableSchema Snake, TableSchema Camel) AdaptSchemaOrThrow(TableSchema foundSchema)
        {
            var adapted = foundSchema.ToAdaptedSchemas();
            if (adapted.Snake == null || adapted.Camel == null ||
                adapted.Snake.Columns == null || adapted.Camel.Columns == null)
            {
                throw new InvalidOperationException($"Не удалось адаптировать схему для {typeof(T).Name}");
            }

            return (adapted.Snake, adapted.Camel);
        }

        private List<DynamicColumnPair> ExtractValidColumnPairs()
        {
            return
            [
                .. _camelSchema.Columns
                    .Zip(_schema.Columns, (camel, snake) => new DynamicColumnPair { Camel = camel, Snake = snake })
                    .Where(p => !string.IsNullOrEmpty(p.Camel.Name) && !string.IsNullOrEmpty(p.Snake.Name))
            ];
        }

        // Вспомогательный служебный класс-контейнер для Zip-итератора
        private class DynamicColumnPair
        {
            public ColumnSchema Camel { get; init; } = null!;
            public ColumnSchema Snake { get; init; } = null!;
        }

        #endregion
    }
}
