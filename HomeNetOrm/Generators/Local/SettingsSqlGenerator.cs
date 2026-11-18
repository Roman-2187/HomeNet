using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Generators.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.Generators.Local
{
    internal class SettingsSqlGenerator : BaseSqlGenerator<SettingsEntity>, ILocalDeviceSettingsSqlGenerator
    {
        public SettingsSqlGenerator(IEnumerable<TableSchema> schemas, ISqlDialectTranslator translator, ICastomLogger logger)
            : base(schemas, translator, logger) { }

        public SqlQueryCommand GenerateSelectActiveSession() =>
            CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where).Col(d => d.IsActive).Emit(OpType.Equal).Param("IsActive", true)
                .Raw(Translator.FormatLimit(1))
                .Build();

        public SqlQueryCommand GenerateSelectById() =>
            CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where).Col(d => d.Id).Emit(OpType.Equal).Param("Id", Guid.Empty)
                .Raw(Translator.FormatLimit(1))
                .Build();

        public SqlQueryCommand GenerateSelectByEmail() =>
          CreateQuery()
              .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
              .Emit(PhraseType.Where).Col(d => d.Email).Emit(OpType.Equal).Param("Email", string.Empty)
              .Raw(Translator.FormatLimit(1))
              .Build();

        public SqlQueryCommand GenerateDeactivateOthers(Guid activeSessionId) =>
            CreateQuery()
                .Emit(CmdType.Update).Table().Emit(PhraseType.Set)
                    .Col(d => d.IsActive).Emit(OpType.Equal).Param("IsActive", false)
                .Emit(PhraseType.Where)
                    .Col(d => d.Id).Emit(OpType.NotEqual).Param("Id", activeSessionId)
                .Build();

      

        public SqlQueryCommand GenerateInsertOrUpdate()
        {
            return CreateQuery()
                .Emit(CmdType.Insert).Emit(PhraseType.Into).Table()
                .InsertFields()
                .Emit(PhraseType.Values)
                .InsertParameters()
                .Emit(PhraseType.On).Emit(PhraseType.Conflict).Emit(OpType.OpenBracket).Col(d => d.UserId).Emit(OpType.CloseBracket)
                .Emit(PhraseType.Do).Emit(CmdType.Update).Emit(PhraseType.Set)
                    .Col(d => d.Id).Emit(OpType.Equal).Raw("EXCLUDED.\"id\"")
                    .Emit(OpType.Comma)
                    .UpsertExcludedFields()
                .Build();
        }


        public SqlQueryCommand GenerateDeleteByUserId(Guid userId) =>
            CreateQuery()
                .Emit(CmdType.Delete).Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where).Col(d => d.UserId).Emit(OpType.Equal).Param("UserId", userId)
                .Build();
    }
}
