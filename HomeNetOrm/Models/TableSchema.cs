namespace HomeNetOrm.Models
{
    public class TableSchema
    {
        public string TableName { get; init; } = string.Empty;
        public List<ColumnSchema> Columns { get; init; } = [];

        // Оставляем только важный флаг логики
        public bool UseVersionTrigger { get; set; } = false;

        // Полезные легковесные хелперы для проверки ключей
        public bool HasCompositePrimaryKey => Columns.Count(c => c.IsPrimaryKey) > 1;
        public ColumnSchema? SinglePrimaryKey => Columns.Count(c => c.IsPrimaryKey) == 1 ? Columns.First(c => c.IsPrimaryKey) : null;
    }
}
