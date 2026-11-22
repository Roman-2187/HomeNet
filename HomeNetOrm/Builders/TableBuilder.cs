using HomeNetOrm.Models;
using System.Linq.Expressions;

namespace HomeNetOrm.Builders
{
    /// <summary>
    /// Билдер схемы таблицы для конкретного класса сущности.
    /// На полном автопилоте собирает структуру без ООП-каши и кэширования строк.
    /// </summary>
    public class TableBuilder<TEntity>(string tableName)
        where TEntity : class
    {
        private readonly List<ColumnBuilder<TEntity>> _columnBuilders = new();
        private bool _useVersionTrigger;

        /// <summary>
        /// Принимает C#-выражение свойства модели (например: x => x.DeviceId)
        /// </summary>
        public ColumnBuilder<TEntity> AddColumn(Expression<Func<TEntity, object?>> propertyExpression)
        {
            ColumnBuilder<TEntity> builder = new(propertyExpression);
            _columnBuilders.Add(builder);
            return builder;
        }

        /// <summary>
        /// Включает для таблицы автоматическое версионирование через Postgres-триггер
        /// </summary>
        public TableBuilder<TEntity> WithVersionTracking()
        {
            _useVersionTrigger = true;
            return this;
        }

        /// <summary>
        /// Финализирует сборку схемы таблицы
        /// </summary>
        public TableSchema Generate()
        {
            List<ColumnSchema> columns = _columnBuilders
                .Select(builder => builder.Build())
                .ToList();

            // 🔥 ЧИСТЫЙ АВТОПИЛОТ: Схема получает только сухие данные структуры
            return new TableSchema
            {
                TableName = tableName,
                Columns = columns,
                UseVersionTrigger = _useVersionTrigger
            };
        }
    }
}
