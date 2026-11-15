using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Generators.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.Generators.Remote
{
    internal class DeletedRecordSqlGenerator : BaseSqlGenerator<DeletedRecordEntity>
    {
        public DeletedRecordSqlGenerator(IEnumerable<TableSchema> schemas, ISqlDialectTranslator translator, ICastomLogger logger)
            : base(schemas, translator, logger) { }

        public SqlQueryCommand GenerateSelectSinceVersion(string entityType, long lastVersion) =>
            CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where)
                    .Col(d => d.EntityType).Emit(OpType.Equal).Param(d => d.EntityType, entityType)
                    .Emit(OpType.And)
                    .Col(d => d.Version).Emit(OpType.GreaterThan).Param(d => d.Version, lastVersion)
                .Build();
    }
}
