namespace HomeNetOrm.Models
{
    public class SchemaDiff
    {
        public string? TableName { get; set; }
        public List<ColumnSchema> MissingColumns { get; set; } = [];
        public List<ColumnSchema> ExtraColumns { get; set; } = [];
        public List<ColumnMismatch> MismatchedColumns { get; set; } = [];

        public bool IsIdentical => MissingColumns.Count == 0 &&
                                  ExtraColumns.Count == 0 &&
                                  MismatchedColumns.Count == 0;
    }


}
