using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNetOrm.Models;

namespace HomeNetOrm.DBProviders.Base
{
    /// <summary>
    /// 🕵️‍♂️ Анализатор и логгер различий в схемах данных.
    /// Полностью изолирует логику форматирования текстовых предупреждений.
    /// </summary>
    internal static class SchemaDiffVisualizer
    {
        public static void LogDifferences(ICastomLogger logger, string dbName, string camelTableName, SchemaDiff? diff, List<ColumnSchema>? expectedCamelCols)
        {
            if (diff == null || diff.IsIdentical) return;

            logger.LogWarning($"[{dbName} ⚠️] ВНИМАНИЕ: Обнаружены расхождения в структуре '{camelTableName}':");

            foreach (ColumnSchema missing in diff.MissingColumns.Where(m => !string.IsNullOrEmpty(m.Name)))
            {
                logger.LogWarning($"   [-] ❌ Отсутствует колонка: {GetLogName(missing.Name)}");
            }

            foreach (ColumnSchema extra in diff.ExtraColumns.Where(e => !string.IsNullOrEmpty(e.Name)))
            {
                logger.LogWarning($"   [+] 🛑 Обнаружена лишняя колонка: {GetLogName(extra.Name)}");
            }

            {
                IEnumerable<ColumnMismatch> validMismatches = diff.MismatchedColumns.Where(m => !string.IsNullOrEmpty(m.ColumnName));
                foreach (ColumnMismatch mismatch in validMismatches)
                {
                    string reason = $"ожидалось {mismatch.Expected.Type}, прилетело {mismatch.Actual.Type}";

                    if (mismatch.Expected.IsNullable != mismatch.Actual.IsNullable)
                        reason += $" [Nullable: {mismatch.Expected.IsNullable} vs {mismatch.Actual.IsNullable}]";

                    if (mismatch.Expected.IsPrimaryKey != mismatch.Actual.IsPrimaryKey)
                        reason += $" [PK: {mismatch.Expected.IsPrimaryKey} vs {mismatch.Actual.IsPrimaryKey}]";

                    logger.LogWarning($"[{dbName}]    [*] 🔄 Расхождение в '{GetLogName(mismatch.ColumnName)}': {reason}");
                }
            }

            return;

            // Локальная функция маппинга регистра
            string GetLogName(string fallbackName) =>
                expectedCamelCols?.FirstOrDefault(c => fallbackName.Equals(c.Name, StringComparison.OrdinalIgnoreCase))?.Name ?? fallbackName;
        }
    }
}
