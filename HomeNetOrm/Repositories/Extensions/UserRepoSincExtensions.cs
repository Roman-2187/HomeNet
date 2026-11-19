using Dapper;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Generators.Extensions; // Подключаем UserSyncSqlGeneratorExtensions
using HomeNetOrm.Repositories.Base;

namespace HomeNetOrm.Repositories.Extensions
{
    public static class UserRepoSincExtensions
    {
        // 📥 Выгрузка пакета измененных профилей для отправки
        public static async Task<IEnumerable<UserEntity>> GetUsersForSyncAsync(this IUserRepo repo, DateTimeOffset checkpoint)
        {
            var baseRepo = (BaseRepository<UserEntity>)repo;
            var cmd = ((IUserSqlGenerator)baseRepo.SqlGenerator).GenerateSelectUsersForSync(checkpoint);

            return await baseRepo.ExecuteAsync(db => db.QueryAsync<UserEntity>(cmd.Sql, cmd.Params));
        }

      


        // В UserRepoSincExtensions.cs:
        public static async Task SaveOrSyncUserAsync(this IUserRepo repo, UserEntity user)
        {
            var baseRepo = (BaseRepository<UserEntity>)repo;
            var cmd = ((IUserSqlGenerator)baseRepo.SqlGenerator).GenerateUserUpsert();

            await baseRepo.ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, user));
            baseRepo.EventBus?.Publish(repo, new IUserRepo.Inserted(user, "Синхронизировано"));
        }


        // 🎯 Универсальная фиксация флага IsSynced = 1 по нашему плоскому методу
        public static async Task MarkUserAsSyncedAsync(this IUserRepo repo, Guid id)
        {
            var baseRepo = (BaseRepository<UserEntity>)repo;
            var cmd = baseRepo.SqlGenerator.GenerateUpdateMarkAsSynced(id);

            await baseRepo.ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, cmd.Params));
            baseRepo.Logger.LogInfo($"[UserEntity] Профиль {id} успешно засинхронен.");
        }


        // 📥 Выгрузка профилей, изменённых после указанной версии


        // 📥 Выгрузка профилей, изменённых после указанной версии
        public static async Task<IEnumerable<UserEntity>> GetUsersSinceVersionAsync(this IUserRepo repo, long sinceVersion)
        {
            var baseRepo = (BaseRepository<UserEntity>)repo;
            var cmd = ((IUserSqlGenerator)baseRepo.SqlGenerator).GenerateSelectUsersSinceVersion(sinceVersion);

            return await baseRepo.ExecuteAsync(db => db.QueryAsync<UserEntity>(cmd.Sql, cmd.Params));
        }



        public static async Task ApplySyncBatchAsync(this IUserRepo repo, IEnumerable<UserEntity> users)
        {
            foreach (var user in users)
            {
                user.IsSynced = 1;
                await repo.SaveOrSyncUserAsync(user);
            }
        }

        public static async Task<List<UserEntity>> GetPendingUsersAsync(this IUserRepo repo)
        {
            var all = await repo.GetAllAsync();
            return all.Where(u => u.IsSynced == 0).ToList();
        }

    }
}
