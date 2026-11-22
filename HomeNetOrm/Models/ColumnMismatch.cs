namespace HomeNetOrm.Models
{
    public class ColumnMismatch
    {
        public string ColumnName { get; init; } = string.Empty;
        public required ColumnSchema Expected { get; init; } 
        public required ColumnSchema Actual { get; init; }
    }
}
