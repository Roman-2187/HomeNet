using Dapper;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Generators.Remote;
using HomeNetOrm.Models;
using System.Data;
using System.Data.Common;

namespace HomeNetOrm.Repositories.Remote
{
    public class DeletedRecordRepository
    {
        private readonly Func<DbConnection> _connectionFactory;
        private readonly DeletedRecordSqlGenerator _sqlGenerator;

        internal DeletedRecordRepository(Func<DbConnection> connectionFactory, DeletedRecordSqlGenerator sqlGenerator)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        }

        public async Task<IEnumerable<DeletedRecordEntity>> GetDeletedSinceVersionAsync(string entityType, long lastVersion)
        {
            await using var db = _connectionFactory();
            if (db.State != ConnectionState.Open) await db.OpenAsync();

            var cmd = _sqlGenerator.GenerateSelectSinceVersion(entityType, lastVersion);
            return await db.QueryAsync<DeletedRecordEntity>(cmd.Sql, new { EntityType = entityType, Version = lastVersion });
        }
    }
}
