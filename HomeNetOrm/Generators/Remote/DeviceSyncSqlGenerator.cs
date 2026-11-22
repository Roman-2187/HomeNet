using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Generators.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators.HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.Generators.Remote
{
    internal class DeviceSyncSqlGenerator(
        IEnumerable<TableSchema> schemas,
        ISqlDialectTranslator translator,
        ICastomLogger logger)
        : BaseSqlGenerator<DeviceSyncEntity>(schemas, translator, logger), IRemoteDeviceSyncSqlGenerator
    {
        /// <summary>
        /// Регистрирует сессию или перезаписывает токен (Id) при повторном входе того же UserId.
        /// </summary>
        public SqlQueryCommand GenerateSessionUpsert()
        {
            return CreateQuery()
                .Emit(Insert).Emit(Into).Table()
                .InsertFields()
                .Emit(Values)
                .InsertParameters()
                .Emit(On).Emit(Conflict)
                    .Emit(OpenBracket).Col(d => d.UserId).Emit(CloseBracket)
                .Emit(Do).Emit(Update).Emit(Set)
                    .Col(d => d.Id).Emit(Equal).Raw("EXCLUDED.\"id\"")
                    .Emit(Comma)
                    .Col(d => d.UpdatedAt).Emit(Equal)
                .Raw(Translator.FormatNow())
                .Build();
        }



        /// <summary>
        /// Создаёт новую независимую сессию устройства без перезаписи существующих.
        /// </summary>
        public SqlQueryCommand GenerateSessionInsert()
        {
            return CreateQuery()
                .Emit(Insert).Emit(Into).Table()
                .InsertFields()
                .Emit(Values)
                .InsertParameters()
                .Build();
        }


        /// <summary>
        /// Получает полную запись сессии по её Id (для проверки авторизации).
        /// </summary>
        public SqlQueryCommand GenerateSelectById(Guid sessionId) =>
            CreateQuery()
                .Emit(Select).Fields().Emit(From).Table()
                .Emit(Where)
                    .Col(d => d.Id).Emit(Equal)
                    .Param(d => d.Id, sessionId)
                .Raw(Translator.FormatLimit(1))
                .Build();

        /// <summary>
        /// Удаляет сессию при выходе пользователя (логауте).
        /// </summary>
        public SqlQueryCommand GenerateDeleteById(Guid sessionId) =>
            CreateQuery()
                .Emit(Delete).Emit(From).Table()
                .Emit(Where)
                    .Col(d => d.Id)
                .Emit(Equal).Param(d => d.Id, sessionId)
                .Build();
    }
}
