using Dapper;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Generators.Local.Sync;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Base;

namespace HomeNetOrm.Repositories.Local.Sync
{
    public static class MessageRepoSincExtensions
    {
        extension(IMessageRepo repo)
        {
            // 📥 Выгрузка хронологии СМС по конкретному чату для репликации
            public async Task<IEnumerable<MessageEntity>> GetMessagesForSyncAsync(Guid userId, DateTimeOffset checkpoint)
            {
                var baseRepo = (BaseRepository<MessageEntity>)repo;
                var cmd = ((IMessageSqlGenerator)baseRepo.SqlGenerator).GenerateSelectMessagesForSync(userId, checkpoint);

                return await baseRepo.ExecuteAsync(db => db.QueryAsync<MessageEntity>(cmd.Sql, cmd.Params));
            }

            public async Task SaveOrSyncMessageAsync(MessageEntity message)
            {
                var baseRepo = (BaseRepository<MessageEntity>)repo;
                var cmd = ((IMessageSqlGenerator)baseRepo.SqlGenerator).GenerateUpsert();

                await baseRepo.ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, message));
            }

            public async Task<IEnumerable<MessageEntity>> GetMessagesSinceVersionAsync(Guid userId, long sinceVersion)
            {
                var baseRepo = (BaseRepository<MessageEntity>)repo;
                var cmd = ((IMessageSqlGenerator)baseRepo.SqlGenerator).GenerateSelectMessagesSinceVersion(userId, sinceVersion);

                return await baseRepo.ExecuteAsync(db => db.QueryAsync<MessageEntity>(cmd.Sql, cmd.Params));
            }

            public async Task ApplySyncBatchAsync(IEnumerable<MessageEntity> messages)
            {
                foreach (var message in messages)
                {
                    message.IsSynced = 1;
                    await repo.SaveOrSyncMessageAsync(message);
                }
            }

            public async Task<IEnumerable<MessageEntity>> GetPendingMessagesAsync()
            {
                var baseRepo = (BaseRepository<MessageEntity>)repo;
                var cmd = ((IMessageSqlGenerator)baseRepo.SqlGenerator).GenerateSelectUnsynced();

                return await baseRepo.ExecuteAsync(db => db.QueryAsync<MessageEntity>(cmd.Sql, cmd.Params));
            }
        }

      
    }
}
