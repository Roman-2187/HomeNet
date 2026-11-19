using Dapper;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Generators.Extensions; // Подключаем UserSyncSqlGeneratorExtensions
using HomeNetOrm.Repositories.Base;

namespace HomeNetOrm.Repositories.Local.Sync
{
    public static class UserRepoSincExtensions
    {
      

        // В UserRepoSincExtensions.cs:
        public static async Task SaveOrSyncUserAsync(this IUserRepo repo, UserEntity user)
        {
            var baseRepo = (BaseRepository<UserEntity>)repo;
            var cmd = ((IUserSqlGenerator)baseRepo.SqlGenerator).GenerateUserUpsert();

            await baseRepo.ExecuteAsync(db => db.ExecuteAsync(cmd.Sql, user));
            baseRepo.EventBus?.Publish(repo, new IUserRepo.Inserted(user, "Синхронизировано"));
        }


      


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
    }
}
