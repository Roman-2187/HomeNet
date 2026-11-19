using Dapper;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Generators.Extensions;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Base;

namespace HomeNetOrm.Repositories.Local.Sync
{
    public static class MessageRepoSincExtensions
    {
        // 📥 Выгрузка хронологии СМС по конкретному чату для репликации
        public static async Task<IEnumerable<MessageEntity>> GetMessagesForSyncAsync(this IMessageRepo repo, Guid userId, DateTimeOffset checkpoint)
        {
            var baseRepo = (BaseRepository<MessageEntity>)repo;
            var cmd = ((IMessageSqlGenerator)baseRepo.SqlGenerator).GenerateSelectMessagesForSync(userId, checkpoint);

            return await baseRepo.ExecuteAsync(db => db.QueryAsync<MessageEntity>(cmd.Sql, cmd.Params));
        }

        // 💾 Атомарное слияние СМС при конфликтах ID (ON CONFLICT DO UPDATE)
        public static async Task SaveOrSyncMessageAsync(this IMessageRepo repo, MessageEntity message)
        {
            var baseRepo = (BaseRepository<MessageEntity>)repo;
            var cmd = ((IMessageSqlGenerator)baseRepo.SqlGenerator).GenerateUpsert();

            await baseRepo.ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, message));
        }

       


        // 📥 Выгрузка сообщений пользователя, изменённых после указанной версии
        public static async Task<IEnumerable<MessageEntity>> GetMessagesSinceVersionAsync(this IMessageRepo repo, Guid userId, long sinceVersion)
        {
            var baseRepo = (BaseRepository<MessageEntity>)repo;
            var cmd = ((IMessageSqlGenerator)baseRepo.SqlGenerator).GenerateSelectMessagesSinceVersion(userId, sinceVersion);

            return await baseRepo.ExecuteAsync(db => db.QueryAsync<MessageEntity>(cmd.Sql, cmd.Params));
        }


   

        public static async Task ApplySyncBatchAsync(this IMessageRepo repo, IEnumerable<MessageEntity> messages)
        {
            foreach (var message in messages)
            {
                message.IsSynced = 1;
                await repo.SaveOrSyncMessageAsync(message);
            }
        }

        public static async Task<IEnumerable<MessageEntity>> GetPendingMessagesAsync(this IMessageRepo repo)
        {
            var baseRepo = (BaseRepository<MessageEntity>)repo;
            var cmd = ((IMessageSqlGenerator)baseRepo.SqlGenerator).GenerateSelectUnsynced();

            return await baseRepo.ExecuteAsync(db => db.QueryAsync<MessageEntity>(cmd.Sql, cmd.Params));
        }

    }
}
