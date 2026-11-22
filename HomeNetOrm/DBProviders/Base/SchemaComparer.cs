using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Base
{
    internal static class SchemaComparer
    {
        private static readonly HashSet<string> NumericAndBoolTypes = new(StringComparer.OrdinalIgnoreCase) { "int", "integer", "bigint", "smallint", "boolean", "bit" };
        private static readonly HashSet<string> TextAndGuidTypes = new(StringComparer.OrdinalIgnoreCase) { "varchar", "text", "string", "character varying", "guid", "uuid" };
        private static readonly HashSet<string> DateTimeTypes = new(StringComparer.OrdinalIgnoreCase) { "datetime", "timestamp", "date" };

        public static SchemaDiff Compare(TableSchema expected, TableSchema actual)
        {
            if (!expected.TableName.Equals(actual.TableName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Нельзя сравнивать разные таблицы: '{expected.TableName}' и '{actual.TableName}'");

            SchemaDiff diff = new SchemaDiff { TableName = expected.TableName };

            Dictionary<string, ColumnSchema> expectedMap = expected.Columns.ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);

            foreach (ColumnSchema actualCol in actual.Columns)
            {
                if (!expectedMap.Remove(actualCol.Name, out ColumnSchema? expectedCol))
                {
                    diff.MissingColumns.Add(actualCol);
                    continue;
                }

                if (!AreColumnsEqual(expectedCol, actualCol))
                {
                    diff.MismatchedColumns.Add(new ColumnMismatch
                    {
                        ColumnName = actualCol.Name,
                        Expected = expectedCol,
                        Actual = actualCol
                    });
                }
            }

            diff.ExtraColumns = expectedMap.Values.ToList();
            return diff;
        }

        private static bool AreColumnsEqual(ColumnSchema expected, ColumnSchema actual)
        {
            if (!string.Equals(expected.Name, actual.Name, StringComparison.OrdinalIgnoreCase))
                return false;

            if (expected.Type != actual.Type)
            {
                if (!IsTypeCompatible(expected.Type.ToString(), actual.Type.ToString()))
                    return false;

                actual.Type = expected.Type;
            }

            if (expected.IsPrimaryKey != actual.IsPrimaryKey)
                return false;

            actual.IsNullable = expected.IsNullable;

            return AreDefaultValuesEqual(expected.DefaultValue, actual.DefaultValue, expected.IsCreatedAt);
        }

        private static bool IsTypeCompatible(string typeA, string typeB)
        {
            string a = typeA.ToLowerInvariant();
            string b = typeB.ToLowerInvariant();

            return (NumericAndBoolTypes.Any(a.Contains) && NumericAndBoolTypes.Any(b.Contains)) ||
                   (TextAndGuidTypes.Any(a.Contains) && TextAndGuidTypes.Any(b.Contains)) ||
                   (DateTimeTypes.Any(a.Contains) && DateTimeTypes.Any(b.Contains));
        }

        private static bool AreDefaultValuesEqual(object? expected, object? actual, bool isCreatedAt)
        {
            string expStr = NormalizeDefaultValue(expected?.ToString() ?? string.Empty);
            string actStr = NormalizeDefaultValue(actual?.ToString() ?? string.Empty);

            if (expStr == actStr) return true;

            if (isCreatedAt && actStr.ToLowerInvariant() is { } lowerAct && (lowerAct.Contains("now") || lowerAct.Contains("current_timestamp")))
                return true;

            return CleanPostgresTypeCasting(expStr).Equals(CleanPostgresTypeCasting(actStr), StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeDefaultValue(string value)
        {
            string str = value?.Trim().Trim('\'', '(', ')') ?? string.Empty;
            return (string.IsNullOrEmpty(str) || str == "0" || str.Equals("false", StringComparison.CurrentCultureIgnoreCase)) ? "0" : str;
        }

        private static string CleanPostgresTypeCasting(string value) =>
            value.Contains("::") ? value.Split(["::"], StringSplitOptions.None)[0].Trim('\'') : value;
    }
}
