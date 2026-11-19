using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Generators.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators.HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.Generators.Remote
{
    internal class DeviceSyncSqlGenerator : BaseSqlGenerator<DeviceSyncEntity>, IRemoteDeviceSyncSqlGenerator
    {
        public DeviceSyncSqlGenerator(IEnumerable<TableSchema> schemas, ISqlDialectTranslator translator, ICastomLogger logger)
            : base(schemas, translator, logger) { }

        /// <summary>
        /// Регистрирует сессию или перезаписывает токен (Id) при повторном входе того же UserId.
        /// </summary>
        public SqlQueryCommand GenerateSessionUpsert()
        {
            return CreateQuery()
                .Emit(CmdType.Insert).Emit(PhraseType.Into).Table()
                .InsertFields()
                .Emit(PhraseType.Values)
                .InsertParameters()
                .Emit(PhraseType.On).Emit(PhraseType.Conflict)
                    .Emit(OpType.OpenBracket).Col(d => d.UserId).Emit(OpType.CloseBracket)
                .Emit(PhraseType.Do).Emit(CmdType.Update).Emit(PhraseType.Set)
                    .Col(d => d.Id).Emit(OpType.Equal).Raw("EXCLUDED.\"id\"")
                    .Emit(OpType.Comma)
                    .Col(d => d.UpdatedAt).Emit(OpType.Equal).Raw(Translator.FormatNow())
                .Build();
        }

        /// <summary>
        /// Получает полную запись сессии по её Id (для проверки авторизации).
        /// </summary>
        public SqlQueryCommand GenerateSelectById(Guid sessionId) =>
            CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where)
                    .Col(d => d.Id).Emit(OpType.Equal)
                    .Param(d => d.Id, sessionId)
                .Raw(Translator.FormatLimit(1))
                .Build();

        /// <summary>
        /// Удаляет сессию при выходе пользователя (логауте).
        /// </summary>
        public SqlQueryCommand GenerateDeleteById(Guid sessionId) =>
            CreateQuery()
                .Emit(CmdType.Delete).Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where)
                    .Col(d => d.Id).Emit(OpType.Equal).Param(d => d.Id, sessionId)
                .Build();
    }
}
