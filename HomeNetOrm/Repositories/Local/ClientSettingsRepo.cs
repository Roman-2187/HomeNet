using Dapper;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Generators.Local;
using HomeNetOrm.Models;
using System.Data.Common;

namespace HomeNetOrm.Repositories.Local
{
    public class ClientSettingsRepo
    {
        private readonly Func<DbConnection> _connectionFactory;
        private readonly SettingsSqlGenerator _sqlGenerator;

        internal ClientSettingsRepo(Func<DbConnection> connectionFactory, SettingsSqlGenerator sqlGenerator)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        }

        public async Task<SettingsEntity?> GetActiveSessionAsync()
        {
            var cmd = _sqlGenerator.GenerateSelectActiveSession();

            using var db = _connectionFactory();
            if (db.State != System.Data.ConnectionState.Open) await db.OpenAsync();
            return await db.QueryFirstOrDefaultAsync<SettingsEntity>(cmd.Sql, cmd.Params);
        }

        public async Task<SettingsEntity?> GetBySessionIdAsync(Guid sessionId)
        {
            var cmd = _sqlGenerator.GenerateSelectById();

            using var db = _connectionFactory();
            if (db.State != System.Data.ConnectionState.Open) await db.OpenAsync();
            return await db.QueryFirstOrDefaultAsync<SettingsEntity>(cmd.Sql, new { Id = sessionId });
        }

        public async Task SaveSettingsAsync(SettingsEntity settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            using var db = _connectionFactory();
            if (db.State != System.Data.ConnectionState.Open) await db.OpenAsync();

            if (settings.IsActive)
            {
                var deactivateCmd = _sqlGenerator.GenerateDeactivateOthers(settings.Id);
                await db.ExecuteAsync(deactivateCmd.Sql, deactivateCmd.Params);
            }

            settings.UpdatedAt = DateTimeOffset.UtcNow;
            SqlQueryCommand insertCmd = _sqlGenerator.GenerateInsertOrUpdate();
            await db.ExecuteAsync(insertCmd.Sql, settings);
        }

        public async Task DeleteByUserIdAsync(Guid userId)
        {
            var cmd = _sqlGenerator.GenerateDeleteByUserId(userId);

            using var db = _connectionFactory();
            if (db.State != System.Data.ConnectionState.Open) await db.OpenAsync();
            await db.ExecuteAsync(cmd.Sql, cmd.Params);
        }


        public async Task<SettingsEntity?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;

            var cmd = _sqlGenerator.GenerateSelectByEmail();

            using var db = _connectionFactory();
            if (db.State != System.Data.ConnectionState.Open) await db.OpenAsync();
            return await db.QueryFirstOrDefaultAsync<SettingsEntity>(cmd.Sql, new { Email = email.Trim() });
        }

    }
}
