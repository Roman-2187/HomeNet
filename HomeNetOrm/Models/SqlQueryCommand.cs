using Dapper;

namespace HomeNetOrm.Models
{
    /// <summary>
    /// 🚀 ОБЪЕКТ ЗАПРОСА: Финальный скомпилированный пакет для СУБД.
    /// Предоставляет строку SQL и параметры для Dapper через точку.
    /// </summary>
    internal class SqlQueryCommand(string sql, DynamicParameters? parameters = null)
    {
        public string Sql { get; } = sql;
        public DynamicParameters? Params { get; } = parameters;
    }
}
