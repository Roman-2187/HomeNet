using Dapper;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Generators.Remote;
using HomeNetOrm.Models;
using System.Data.Common;

namespace HomeNetOrm.Repositories.Remote
{
    public class DeviceSyncRepository
    {
        private readonly Func<DbConnection> _connectionFactory;
        private readonly DeviceSyncSqlGenerator _sqlGenerator;

        internal DeviceSyncRepository(Func<DbConnection> connectionFactory, DeviceSyncSqlGenerator sqlGenerator)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        }

        /// <summary>
        /// Создаёт или обновляет сессию при входе/регистрации.
        /// </summary>
        public async Task<DeviceSyncEntity> UpsertSessionAsync(Guid sessionId, Guid userId)
        {
            var now = DateTimeOffset.UtcNow;
            var syncData = new DeviceSyncEntity
            {
                Id = sessionId,
                UserId = userId,
                CreatedAt = now,
                UpdatedAt = now
            };

            SqlQueryCommand cmd = _sqlGenerator.GenerateSessionUpsert();

            using var db = _connectionFactory();
            if (db.State != System.Data.ConnectionState.Open) await db.OpenAsync();
            await db.ExecuteAsync(cmd.Sql, syncData);

            return (await GetByIdAsync(sessionId))!;
        }

        /// <summary>
        /// Ищет активную сессию по её Id. Если вернётся null — запрос не авторизован.
        /// </summary>
        public async Task<DeviceSyncEntity?> GetByIdAsync(Guid sessionId)
        {
            SqlQueryCommand cmd = _sqlGenerator.GenerateSelectById(sessionId);

            using var db = _connectionFactory();
            if (db.State != System.Data.ConnectionState.Open) await db.OpenAsync();
            return await db.QueryFirstOrDefaultAsync<DeviceSyncEntity>(cmd.Sql, cmd.Params);
        }

        /// <summary>
        /// Аннулирует сессию при выходе из аккаунта.
        /// </summary>
        public async Task DeleteSessionAsync(Guid sessionId)
        {
            SqlQueryCommand cmd = _sqlGenerator.GenerateDeleteById(sessionId);

            using var db = _connectionFactory();
            if (db.State != System.Data.ConnectionState.Open) await db.OpenAsync();
            await db.ExecuteAsync(cmd.Sql, cmd.Params);
        }
    }
}
