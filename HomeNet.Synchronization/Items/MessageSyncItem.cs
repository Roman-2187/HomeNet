using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Network.Api;
using HomeNet.Synchronization.Interfaces.Sinc;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Local;
using HomeNetOrm.Repositories.Local.Sync;

namespace HomeNet.Synchronization.Items
{
    public class MessageSyncItem(
        IMessageRepo messageRepository,
        DeviceSettingsRepo deviceSettingsRepo,
        IMessageApiClient apiClient,
        ICastomLogger logger) : IEntitySyncItem
    {
        public int Order => 2;

        public async Task PullAsync(CancellationToken ct = default)
        {
            try
            {
                var session = await deviceSettingsRepo.GetActiveSessionAsync();
                if (session == null || session.UserId == Guid.Empty) return;

                var batch = await apiClient.FetchFreshMessagesAsync(session.LastMessageVersion);

                await messageRepository.ApplySyncBatchAsync(batch.UpdatedItems);

                if (batch.LatestVersion > session.LastMessageVersion)
                {
                    await deviceSettingsRepo.UpdateLastMessageVersionAsync(session.UserId, batch.LatestVersion);
                    logger.LogInfo($"[MessageSyncItem] Курсор сообщений обновлён: {session.LastMessageVersion} -> {batch.LatestVersion}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"[MessageSyncItem] Ошибка PULL: {ex.Message}");
            }
        }

        public async Task PushAsync(CancellationToken ct = default)
        {
            try
            {
                var pending = await messageRepository.GetPendingMessagesAsync();
                foreach (var message in pending)
                {
                    await apiClient.PushMessageAsync(message);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"[MessageSyncItem] Ошибка PUSH: {ex.Message}");
            }
        }
    }
}
