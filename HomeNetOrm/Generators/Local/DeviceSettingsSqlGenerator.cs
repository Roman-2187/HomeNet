using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Generators.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.Generators.Local
{
    internal class DeviceSettingsSqlGenerator : BaseSqlGenerator<DeviceSettingsEntity>, ILocalDeviceSettingsSqlGenerator
    {
        public DeviceSettingsSqlGenerator(IEnumerable<TableSchema> schemas, ISqlDialectTranslator translator, ICastomLogger logger)
            : base(schemas, translator, logger) { }

        /// <summary>
        /// Выбирает активную сессию на устройстве (где IsActive = true).
        /// </summary>
        public SqlQueryCommand GenerateSelectActiveSession() =>
            CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where).Col(d => d.IsActive).Emit(OpType.Equal).Param("IsActive", true)
                .Raw(Translator.FormatLimit(1))
                .Build();

        /// <summary>
        /// Выбирает конкретный аккаунт по его детерминированному Id (хэш DeviceId + UserId).
        /// </summary>
        public SqlQueryCommand GenerateSelectById() =>
            CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where).Col(d => d.Id).Emit(OpType.Equal).Param("Id", Guid.Empty)
                .Raw(Translator.FormatLimit(1))
                .Build();

        /// <summary>
        /// UPSERT по детерминированному ключу Id (паре DeviceId + UserId).
        /// </summary>
        public SqlQueryCommand GenerateInsertOrUpdate()
        {
            return CreateQuery()
                .Emit(CmdType.Insert).Emit(PhraseType.Into).Table()
                .InsertFields()
                .Emit(PhraseType.Values)
                .InsertParameters()
                .Emit(PhraseType.On).Emit(PhraseType.Conflict).Emit(OpType.OpenBracket).Col(d => d.Id).Emit(OpType.CloseBracket)
                .Emit(PhraseType.Do).Emit(CmdType.Update).Emit(PhraseType.Set)
                .UpsertExcludedFields()
                .Build();
        }
    }
}
