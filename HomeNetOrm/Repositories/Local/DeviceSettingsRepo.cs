using Dapper;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Generators.Local;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Models;
using System.Data;
using System.Data.Common;

namespace HomeNetOrm.Repositories.Local
{
    public class DeviceSettingsRepo : IDeviceSettingsRepo
    {
        private readonly Func<DbConnection> _connectionFactory;
        private readonly SettingsSqlGenerator _sqlGenerator;

        internal DeviceSettingsRepo(Func<DbConnection> connectionFactory, SettingsSqlGenerator sqlGenerator)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        }

        public async Task<SettingsEntity?> GetActiveSessionAsync()
        {
            SqlQueryCommand cmd = _sqlGenerator.GenerateSelectActiveSession();

            await using DbConnection db = _connectionFactory();
            if (db.State != ConnectionState.Open) await db.OpenAsync();
            return await db.QueryFirstOrDefaultAsync<SettingsEntity>(cmd.Sql, cmd.Params);
        }

        public async Task<SettingsEntity?> GetBySessionIdAsync(Guid sessionId)
        {
            SqlQueryCommand cmd = _sqlGenerator.GenerateSelectById();

            await using DbConnection db = _connectionFactory();
            if (db.State != ConnectionState.Open) await db.OpenAsync();
            return await db.QueryFirstOrDefaultAsync<SettingsEntity>(cmd.Sql, new { Id = sessionId });
        }

        public async Task<SettingsEntity?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;

            SqlQueryCommand cmd = _sqlGenerator.GenerateSelectByEmail();

            await using DbConnection db = _connectionFactory();
            if (db.State != ConnectionState.Open) await db.OpenAsync();
            return await db.QueryFirstOrDefaultAsync<SettingsEntity>(cmd.Sql, new { Email = email.Trim() });
        }

        public async Task SaveSettingsAsync(SettingsEntity settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            await using DbConnection db = _connectionFactory();
            if (db.State != ConnectionState.Open) await db.OpenAsync();

            if (settings.LastUserVersion == 0 && settings.LastMessageVersion == 0 && !string.IsNullOrWhiteSpace(settings.Email))
            {
                SettingsEntity? existing = await GetByEmailAsync(settings.Email);
                if (existing != null)
                {
                    settings.LastUserVersion = existing.LastUserVersion;
                    settings.LastMessageVersion = existing.LastMessageVersion;
                }
            }

            if (settings.IsActive)
            {
                SqlQueryCommand deactivateCmd = _sqlGenerator.GenerateDeactivateOthers(settings.Id);
                await db.ExecuteAsync(deactivateCmd.Sql, deactivateCmd.Params);
            }

            settings.UpdatedAt = DateTimeOffset.UtcNow;
            SqlQueryCommand insertCmd = _sqlGenerator.GenerateInsertOrUpdate();
            await db.ExecuteAsync(insertCmd.Sql, settings);
        }

        public async Task UpdateLastUserVersionAsync(Guid userId, long newVersion)
        {
            SqlQueryCommand cmd = _sqlGenerator.GenerateUpdateLastUserVersion(userId, newVersion);

            await using DbConnection db = _connectionFactory();
            if (db.State != ConnectionState.Open) await db.OpenAsync();
            await db.ExecuteAsync(cmd.Sql, cmd.Params);
        }

        public async Task UpdateLastMessageVersionAsync(Guid userId, long newVersion)
        {
            SqlQueryCommand cmd = _sqlGenerator.GenerateUpdateLastMessageVersion(userId, newVersion);

            await using DbConnection db = _connectionFactory();
            if (db.State != ConnectionState.Open) await db.OpenAsync();
            await db.ExecuteAsync(cmd.Sql, cmd.Params);
        }

        public async Task DeleteByUserIdAsync(Guid userId)
        {
            SqlQueryCommand cmd = _sqlGenerator.GenerateDeleteByUserId(userId);

            await using DbConnection db = _connectionFactory();
            if (db.State != ConnectionState.Open) await db.OpenAsync();
            await db.ExecuteAsync(cmd.Sql, cmd.Params);
        }
    }
}
