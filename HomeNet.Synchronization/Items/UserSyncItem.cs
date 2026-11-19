using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.Api;
using HomeNet.Synchronization.Interfaces.Sinc;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Extensions;
using HomeNetOrm.Repositories.Local;

namespace HomeNet.Synchronization.Items
{
    public class UserSyncItem(
        IUserRepo userRepository,
        ClientSettingsRepo deviceSettingsRepo,
        IUserApiClient apiClient,
        ICastomLogger logger) : IEntitySyncItem
    {
        public int Order => 1;

        public async Task PullAsync(CancellationToken ct = default)
        {
            try
            {
                var session = await deviceSettingsRepo.GetActiveSessionAsync();
                if (session == null || session.Id == Guid.Empty) return;

                var batch = await apiClient.FetchFreshUsersAsync(session.LastUserVersion);

                await userRepository.ApplySyncBatchAsync(batch.UpdatedItems);
                session = await ApplyTombstonesAsync(batch.DeletedIds, session);

                if (session != null && batch.LatestVersion > session.LastUserVersion)
                {
                    await deviceSettingsRepo.UpdateLastUserVersionAsync(session.UserId, batch.LatestVersion);
                    logger.LogInfo($"[UserSyncItem] Курсор профилей обновлён: {session.LastUserVersion} -> {batch.LatestVersion}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"[UserSyncItem] Ошибка PULL: {ex.Message}");
            }
        }

        public async Task PushAsync(CancellationToken ct = default)
        {
            try
            {
                var pending = await userRepository.GetPendingUsersAsync();
                foreach (var user in pending)
                {
                    await apiClient.PushUserAsync(user);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"[UserSyncItem] Ошибка PUSH: {ex.Message}");
            }
        }

        private async Task<SettingsEntity?> ApplyTombstonesAsync(IEnumerable<Guid> deletedIds, SettingsEntity? session)
        {
            foreach (var id in deletedIds)
            {
                await userRepository.DeleteByIdAsync(id);
                await deviceSettingsRepo.DeleteByUserIdAsync(id);
                if (session?.UserId == id) session = null;
            }
            return session;
        }
    }
}
