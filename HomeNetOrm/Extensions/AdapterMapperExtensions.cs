using HomeNetOrm.Enums;
using HomeNetOrm.Models;
using System.Linq.Expressions;
using System.Reflection;

namespace HomeNetOrm.Extensions
{
    /// <summary>
    /// Статический движок маппинга: расширяет функционал адаптера схем, 
    /// выковыривая и сопоставляя C# типы свойств с внутренними типами СУБД.
    /// </summary>
    public static class AdapterMapperExtensions
    {
        /// <summary>
        /// Разбирает C# Lambda-выражение свойства и маппит его во внутренний ColumnSchema со строгим типом ORM.
        /// </summary>
        public static ColumnSchema MapPropertySchema<TEntity>(this Expression<Func<TEntity, object?>> expression) where TEntity : class
        {
            ArgumentNullException.ThrowIfNull(expression);

            var memberExpression = expression.Body as MemberExpression;

            // Снимаем Boxing-обертку для значимых типов (int, bool, DateTime, Guid, Enums)
            if (memberExpression == null && expression.Body is UnaryExpression unaryExpression)
            {
                memberExpression = unaryExpression.Operand as MemberExpression;
            }

            if (memberExpression is not { Member: PropertyInfo propertyInfo })
            {
                throw new ArgumentException($"Выражение должно ссылаться на валидное свойство класса {typeof(TEntity).Name}.");
            }

            // 🔥 ФИКС: Убираем затертый OriginalName. Оставляем только чистое C# имя поля
            var schema = new ColumnSchema
            {
                Name = propertyInfo.Name // Адаптер в слое конвертации позже переведет это в нужный регистр на лету 🐍
            };

            // Распаковываем базовый тип, если свойство обернуто в Nullable<T>
            var propertyType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

            // 🔥 АВТОМАТИЗАЦИЯ ЕНАМОВ: Если свойство является Enum — сохраняем его в БД как Integer (число)
            if (propertyType.IsEnum)
            {
                schema.Type = ColumnType.Integer;
                return schema;
            }

            // Прямое сопоставление фундаментальных типов C# и ColumnType движка
            schema.Type = propertyType switch
            {
                not null when propertyType == typeof(int) || propertyType == typeof(long) => ColumnType.Integer,
                not null when propertyType == typeof(string) => ColumnType.Varchar,
                not null when propertyType == typeof(bool) => ColumnType.Boolean,

                // Наш бро для кросс-базовой синхронизации (SQLite + Postgres) ⚡
                not null when propertyType == typeof(Guid) => ColumnType.Guid,

                // ⏱ ТОТАЛЬНЫЙ DateTimeOffset: 
                // Заставляем любые типы даты-времени C# жестко маппиться в тип со смещением!
                not null when propertyType == typeof(DateTime) => ColumnType.DateTimeOffset,
                not null when propertyType == typeof(DateTimeOffset) => ColumnType.DateTimeOffset,

                _ => ColumnType.Unspecified
            };

            return schema;
        }
    }
}
