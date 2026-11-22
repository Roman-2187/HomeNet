using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Generators.Base;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;

namespace HomeNetOrm.Generators.Remote
{
    internal class DeletedRecordSqlGenerator(
        IEnumerable<TableSchema> schemas,
        ISqlDialectTranslator translator,
        ICastomLogger logger)
        : BaseSqlGenerator<DeletedRecordEntity>(schemas, translator, logger)
    {
        public SqlQueryCommand GenerateSelectSinceVersion(string entityType, long lastVersion) =>
            CreateQuery()
                .Emit(Select).Fields().Emit(From).Table()
                .Emit(Where)
                    .Col(d => d.EntityType).Emit(Equal).Param(d => d.EntityType, entityType)
                    .Emit(And)
                    .Col(d => d.Version).Emit(GreaterThan).Param(d => d.Version, lastVersion)
                .Build();
    }
}
