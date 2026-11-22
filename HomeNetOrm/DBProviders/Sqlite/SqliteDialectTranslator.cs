using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.DBProviders.Sqlite
{
    public class SqliteDialectTranslator : ISqlDialectTranslator
    {
        // --- 🏗️ Блок экранирования метаданных (Таблицы и Колонки) ---

        public string FormatTable(TableSchema schema) => $"[{schema.TableName}]";
        public string FormatColumnName(string columnName) => $"[{columnName}]";

        public string FormatReturningId(string columnName) => $"; SELECT last_insert_rowid() AS [{columnName}]";

        public string FormatLimit(int limit, int? offset = null) =>
            offset.HasValue ? $"LIMIT {limit} OFFSET {offset}" : $"LIMIT {limit}";

        // --- ⚡ Системные функции и кастинг типов SQLite ---

        public string FormatCastToText(string columnName) => $"CAST([{columnName}] AS TEXT)";

        public string FormatCountAll() => "COUNT(*)";
        public string FormatCountOne() => "COUNT(*)";

        // 🔥 Динамическое получение текущего времени в SQLite через нативную функцию
        public string FormatNow() => SqliteNowFunc;

        // --- 🧱 Блок трансляции перечислений из DTO Sql ---

        // 🟥 1. Команды (DML / DDL)
        public string Translate(CmdType cmd) => cmd switch
        {
            Select => "SELECT",
            Update => "UPDATE",
            Delete => "DELETE",
            Insert => "INSERT",
            Create => "CREATE",
            CmdType.Trigger => "TRIGGER",
            Function => "CREATE FACTORY",
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
            // 🔥 Дописываем кейсы для триггерных структур
            Sequence => "SEQUENCE",
            OrDml => "OR",
            _ => throw new ArgumentOutOfRangeException(nameof(phrase))
        };

        // 🟩 3. Операторы и логика (Истина/Ложь в SQLite — это 1 и 0)
        public string Translate(OpType op) => op switch
        {
            Equal => "=",
            And => "AND",
            Or => "OR",
            True => "1",
            False => "0",
            OpenBracket => "(",
            CloseBracket => ")",
            Asc => "ASC",
            Desc => "DESC",
            GreaterThan => ">",
            IsNull => "IS NULL",
            Begin => "BEGIN",
            End => "END;",
            
            NotEqual => "!=",
            Comma => ",",
            Excluded => "EXCLUDED", // 🆕 SQLite поймет конструкцию EXCLUDED.field один в один!
            CurrentTimestamp => "CURRENT_TIMESTAMP", // 🆕 Встроенное время SQLite (вернет текущую дату-время)
            _ => throw new ArgumentOutOfRangeException(nameof(op)) //
        };

        // 🟦 4. Специфика триггеров под диалект SQLite
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
            TriggerType.Execute => "BEGIN",
            TriggerType.New => "NEW",
            TriggerType.Old => "OLD",
            _ => throw new ArgumentOutOfRangeException(nameof(trig))
        };

        // =========================================================================
        // 🧰 ПОДВАЛ КЛАССА: Изолируем специфичные текстовые маркеры SQLite
        // =========================================================================
        //private const string SqliteNowFunc = "datetime('now')";

       private const string SqliteNowFunc = "strftime('%Y-%m-%dT%H:%M:%fZ', 'now')";
    }
}
