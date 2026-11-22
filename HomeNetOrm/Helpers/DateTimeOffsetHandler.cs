using Dapper;
using System.Data;
using System.Globalization;

namespace HomeNetOrm.Helpers
{
    /// <summary>
    /// Специальный обработчик типов для Dapper: обучает его безболезненно парсить 
    /// любые длинные текстовые строки дат из SQLite в C# тип DateTimeOffset.
    /// </summary>
    public class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
        {
            // Сохраняем в СУБД в стандартизированном ISO-формате строки
            parameter.Value = value.ToString("o");
        }

        public override DateTimeOffset Parse(object value)
        {
            return value switch
            {
                // Если из SQLite прилетела строка — парсим её стандартными средствами .NET,
                // которые, в отличие от Dapper, переваривают любую длину миллисекунд!
                string str when DateTimeOffset.TryParse(str, CultureInfo.InvariantCulture, out var parsed) =>
                    parsed,
                DateTime dt => new DateTimeOffset(dt),
                _ => DateTimeOffset.MinValue
            };
        }
    }
}
