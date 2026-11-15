using Dapper;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Generators.Local;
using HomeNetOrm.Models;
using System.Data.Common;

namespace HomeNetOrm.Repositories.Local
{
    public class ClientDeviceSettingsRepository
    {
        private readonly Func<DbConnection> _connectionFactory;
        private readonly DeviceSettingsSqlGenerator _sqlGenerator;

        internal ClientDeviceSettingsRepository(Func<DbConnection> connectionFactory, DeviceSettingsSqlGenerator sqlGenerator)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        }

        /// <summary>
        /// Ищет текущую активную сессию на устройстве (для автовхода при запуске).
        /// </summary>
        public async Task<DeviceSettingsEntity?> GetActiveSessionAsync()
        {
            using var db = _connectionFactory();
            if (db.State != System.Data.ConnectionState.Open) await db.OpenAsync();

            const string sql = "SELECT * FROM device_settings WHERE is_active = 1 LIMIT 1;";
            return await db.QueryFirstOrDefaultAsync<DeviceSettingsEntity>(sql);
        }


        public async Task DeleteByUserIdAsync(Guid userId)
        {
            using var db = _connectionFactory();
            if (db.State != System.Data.ConnectionState.Open) await db.OpenAsync();

            var cmd = _sqlGenerator.GenerateDeleteByUserId(userId);
            await db.ExecuteAsync(cmd.Sql, cmd.Params);
        }


        /// <summary>
        /// Находит сохранённую запись конкретного аккаунта по детерминированному SessionId.
        /// </summary>
        public async Task<DeviceSettingsEntity?> GetBySessionIdAsync(Guid sessionId)
        {
            using var db = _connectionFactory();
            if (db.State != System.Data.ConnectionState.Open) await db.OpenAsync();

            const string sql = "SELECT * FROM device_settings WHERE id = @Id LIMIT 1;";
            return await db.QueryFirstOrDefaultAsync<DeviceSettingsEntity>(sql, new { Id = sessionId });
        }

        /// <summary>
        /// Возвращает количество уникальных аккаунтов, когда-либо заходивших с этого устройства.
        /// </summary>
        public async Task<int> GetKnownAccountsCountAsync()
        {
            using var db = _connectionFactory();
            if (db.State != System.Data.ConnectionState.Open) await db.OpenAsync();

            const string sql = "SELECT COUNT(*) FROM device_settings;";
            return await db.ExecuteScalarAsync<int>(sql);
        }

        /// <summary>
        /// Сохраняет/обновляет запись аккаунта через твой родной UPSERT-генератор.
        /// </summary>
        public async Task SaveSettingsAsync(DeviceSettingsEntity settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            using var db = _connectionFactory();
            if (db.State != System.Data.ConnectionState.Open) await db.OpenAsync();

            // Если эта сессия становится активной — деактивируем остальные на этом устройстве
            if (settings.IsActive)
            {
                await db.ExecuteAsync("UPDATE device_settings SET is_active = 0 WHERE id != @Id;", new { settings.Id });
            }

            settings.UpdatedAt = DateTimeOffset.UtcNow;
            SqlQueryCommand insertCmd = _sqlGenerator.GenerateInsertOrUpdate();
            await db.ExecuteAsync(insertCmd.Sql, settings);
        }
    }
}
