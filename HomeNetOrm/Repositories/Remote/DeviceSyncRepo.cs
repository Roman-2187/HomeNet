using Dapper;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Generators.Remote;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Models;
using System.Data;
using System.Data.Common;

namespace HomeNetOrm.Repositories.Remote
{
    public class DeviceSyncRepo : IDeviceSyncRepo
    {
        private readonly Func<DbConnection> _connectionFactory;
        private readonly DeviceSyncSqlGenerator _sqlGenerator;

        internal DeviceSyncRepo(Func<DbConnection> connectionFactory, DeviceSyncSqlGenerator sqlGenerator)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        }

        /// <summary>
        /// Создаёт или обновляет сессию при входе/регистрации.
        /// </summary>
        public async Task<DeviceSyncEntity> UpsertSessionAsync(Guid sessionId, Guid userId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DeviceSyncEntity syncData = new DeviceSyncEntity
            {
                Id = sessionId,
                UserId = userId,
                CreatedAt = now,
                UpdatedAt = now
            };

            SqlQueryCommand cmd = _sqlGenerator.GenerateSessionUpsert();

            await using DbConnection db = _connectionFactory();
            if (db.State != ConnectionState.Open) await db.OpenAsync();
            await db.ExecuteAsync(cmd.Sql, syncData);

            return (await GetByIdAsync(sessionId))!;
        }

        /// <summary>
        /// Ищет активную сессию по её Id. Если вернётся null — запрос не авторизован.
        /// </summary>
        public async Task<DeviceSyncEntity?> GetByIdAsync(Guid sessionId)
        {
            SqlQueryCommand cmd = _sqlGenerator.GenerateSelectById(sessionId);

            await using DbConnection db = _connectionFactory();
            if (db.State != ConnectionState.Open) await db.OpenAsync();
            return await db.QueryFirstOrDefaultAsync<DeviceSyncEntity>(cmd.Sql, cmd.Params);
        }



        /// <summary>
        /// Создаёт новую независимую сессию при входе с устройства.
        /// </summary>
        public async Task<DeviceSyncEntity> CreateSessionAsync(Guid sessionId, Guid userId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DeviceSyncEntity syncData = new DeviceSyncEntity
            {
                Id = sessionId,
                UserId = userId,
                CreatedAt = now,
                UpdatedAt = now
            };

            SqlQueryCommand cmd = _sqlGenerator.GenerateSessionInsert();

            await using DbConnection db = _connectionFactory();
            if (db.State != ConnectionState.Open) await db.OpenAsync();
            await db.ExecuteAsync(cmd.Sql, syncData);

            return (await GetByIdAsync(sessionId))!;
        }


        /// <summary>
        /// Аннулирует сессию при выходе из аккаунта.
        /// </summary>
        public async Task DeleteSessionAsync(Guid sessionId)
        {
            SqlQueryCommand cmd = _sqlGenerator.GenerateDeleteById(sessionId);

            await using DbConnection db = _connectionFactory();
            if (db.State != ConnectionState.Open) await db.OpenAsync();
            await db.ExecuteAsync(cmd.Sql, cmd.Params);
        }
    }
}
