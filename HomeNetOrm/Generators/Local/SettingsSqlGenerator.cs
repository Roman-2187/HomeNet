using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Generators.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.Generators.Local
{
    internal class SettingsSqlGenerator(
        IEnumerable<TableSchema> schemas,
        ISqlDialectTranslator translator,
        ICastomLogger logger)
        : BaseSqlGenerator<SettingsEntity>(schemas, translator, logger), ILocalDeviceSettingsSqlGenerator
    {
        public SqlQueryCommand GenerateSelectActiveSession() =>
            CreateQuery()
                .Emit(Select).Fields().Emit(From).Table()
                .Emit(Where).Col(d => d.IsActive).Emit(Equal).Param(d => d.IsActive, true)
                .Raw(Translator.FormatLimit(1))
                .Build();

        public SqlQueryCommand GenerateSelectById() =>
            CreateQuery()
                .Emit(Select).Fields().Emit(From).Table()
                .Emit(Where).Col(d => d.Id).Emit(Equal).Param(d => d.Id, Guid.Empty)
                .Raw(Translator.FormatLimit(1))
                .Build();

        public SqlQueryCommand GenerateSelectByEmail() =>
            CreateQuery()
                .Emit(Select).Fields().Emit(From).Table()
                .Emit(Where).Col(d => d.Email).Emit(Equal).Param(d => d.Email, string.Empty)
                .Raw(Translator.FormatLimit(1))
                .Build();

        public SqlQueryCommand GenerateDeactivateOthers(Guid activeSessionId) =>
            CreateQuery()
                .Emit(Update).Table().Emit(Set)
                .Col(d => d.IsActive).Emit(Equal).Param(d => d.IsActive, false)
                .Emit(Where)
                .Col(d => d.Id).Emit(NotEqual).Param(d => d.Id, activeSessionId)
                .Build();

        public SqlQueryCommand GenerateUpdateLastUserVersion(Guid userId, long newVersion) =>
            CreateQuery()
                .Emit(Update).Table().Emit(Set)
                .Col(d => d.LastUserVersion).Emit(Equal).Param(d => d.LastUserVersion, newVersion)
                .Emit(Comma)
                .Col(d => d.UpdatedAt).Emit(Equal).Param(d => d.UpdatedAt, DateTimeOffset.UtcNow)
                .Emit(Where)
                .Col(d => d.UserId).Emit(Equal).Param(d => d.UserId, userId)
                .Build();

        public SqlQueryCommand GenerateUpdateLastMessageVersion(Guid userId, long newVersion) =>
            CreateQuery()
                .Emit(Update).Table().Emit(Set)
                .Col(d => d.LastMessageVersion).Emit(Equal).Param(d => d.LastMessageVersion, newVersion)
                .Emit(Comma)
                .Col(d => d.UpdatedAt).Emit(Equal).Param(d => d.UpdatedAt, DateTimeOffset.UtcNow)
                .Emit(Where)
                .Col(d => d.UserId).Emit(Equal).Param(d => d.UserId, userId)
                .Build();

        public SqlQueryCommand GenerateInsertOrUpdate() =>
            CreateQuery()
                .Emit(Insert).Emit(Into).Table()
                .InsertFields()
                .Emit(Values)
                .InsertParameters()
                .Emit(On).Emit(Conflict).Emit(OpenBracket).Col(d => d.UserId).Emit(CloseBracket)
                .Emit(Do).Emit(Update).Emit(Set)
                .Col(d => d.Id).Emit(Equal).Emit(Excluded).Raw(".").Col(d => d.Id)
                .Emit(Comma)
                .UpsertExcludedFields()
                .Build();

        public SqlQueryCommand GenerateDeleteByUserId(Guid userId) =>
            CreateQuery()
                .Emit(Delete).Emit(From).Table()
                .Emit(Where).Col(d => d.UserId).Emit(Equal).Param(d => d.UserId, userId)
                .Build();
    }
}