namespace HomeNetOrm.Models
{

    /// <summary>
    /// Плоская модель для автоматического маппинга метаданных СУБД через Dapper.
    /// </summary>
    public class RawColumnMetadata
    {
        public string Name { get; init; } = string.Empty;
        public string DataType { get; init; } = string.Empty;

        // ⚡ Ставим bool, так как мы скорректировали SQL под выдачу флагов 1/0 и true/false
        public bool IsNullable { get; init; }

        public string KeyType { get; init; } = string.Empty;
        public string ExtraInfo { get; init; } = string.Empty;

        // Пустой конструктор для Dapper — теперь он скомпилирует объект без единого писка!
    }
}


