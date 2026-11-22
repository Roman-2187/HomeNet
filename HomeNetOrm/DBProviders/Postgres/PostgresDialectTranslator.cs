using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.DBProviders.Postgres
{
    public class PostgresDialectTranslator : ISqlDialectTranslator
    {
        // --- 🏗️ Блок экранирования метаданных (Таблицы и Колонки) ---

        public string FormatTable(TableSchema schema) => $"\"{GenericSchemaAdapter.ToSnakeCase(schema.TableName)}\"";

        public string FormatColumnName(string columnName) => $"\"{columnName}\"";

        public string FormatReturningId(string columnName) => $"RETURNING \"{columnName}\"";

        public string FormatLimit(int limit, int? offset = null) =>
            offset.HasValue ? $"LIMIT {limit} OFFSET {offset}" : $"LIMIT {limit}";

        // --- ⚡ Системные функции и кастинг типов Postgres ---

        public string FormatCastToText(string expression) => $"{expression}::text";

        public string FormatCountAll() => "COUNT(*)";

        public string FormatCountOne() => "COUNT(1)";

        // 🔥 Наш новый метод динамического получения текущего времени СУБД
        public string FormatNow() => PostgresNowFunc;

        // --- 🧱 Блок трансляции compact перечислений из DTO Sql ---

        // 🟥 1. Команды (DML / DDL)
        public string Translate(CmdType cmd) => cmd switch
        {
            Select => "SELECT",
            Update => "UPDATE",
            Delete => "DELETE",
            Insert => "INSERT",
            Create => "CREATE",
            CmdType.Trigger => "TRIGGER",
            Function => "FUNCTION",
            Procedure => "PROCEDURE",
            _ => throw new ArgumentOutOfRangeException(nameof(cmd))
        };

        // 🟨 2. Ключевые слова-связки
        public string Translate(PhraseType phrase) => phrase switch
        {
            From => "FROM",
            Where => "WHERE",
            Set => "SET",
            Into => "INTO",
            Values => "VALUES",
            Returning => "RETURNING",
            OrderBy => "ORDER BY",
            Language => "LANGUAGE",
            On => "ON",
            Conflict => "CONFLICT",
            Do => "DO",
            As => "AS",
            // 🔥 Дописываем новые кейсы для триггерных расширений схем:
            Sequence => "SEQUENCE",
            OrDml => "OR",
            _ => throw new ArgumentOutOfRangeException(nameof(phrase))
        };

        // 🟩 3. Операторы, логика и границы блоков
        public string Translate(OpType op) => op switch
        {
            Equal => "=",
            NotEqual => "!=",
            And => "AND",
            Or => "OR",
            True => "TRUE",
            False => "FALSE",
            Null => "NULL",
            OpenBracket => "(",
            CloseBracket => ")",
            Asc => "ASC",
            Desc => "DESC",
            GreaterThan => ">",
            IsNull => "IS NULL",
            Begin => "BEGIN",
            End => "END;",
            Comma => ",",
            Excluded => "EXCLUDED", // 🆕 Подставляется перед точкой в блоке SET
            CurrentTimestamp => "CURRENT_TIMESTAMP", // 🆕 Встроенное время Postgres
            _ => throw new ArgumentOutOfRangeException(nameof(op)) //
           
        };

        // 🟦 4. Специфика триггеров
        public string Translate(TriggerType trig) => trig switch
        {
            TriggerType.Returns => "RETURNS",
            TriggerType.Replace => "OR REPLACE",
            TriggerType.Before => "BEFORE",
            TriggerType.After => "AFTER",
            TriggerType.InsteadOf => "INSTEAD OF",
            TriggerType.On => "ON",
            TriggerType.ForEach => "FOR EACH",
            TriggerType.Row => "ROW",
            TriggerType.Execute => "EXECUTE FUNCTION",
            TriggerType.New => "NEW",
            TriggerType.Old => "OLD",
            _ => throw new ArgumentOutOfRangeException(nameof(trig))
        };

        // =========================================================================
        // 🧰 ПОДВАЛ КЛАССА: Изолируем специфичные текстовые маркеры Postgres
        // =========================================================================
        private const string PostgresNowFunc = "NOW()";
        private const string ProceduralLanguageMarker = "plpgsql";
    }
}
