using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Network.Api;
using HomeNet.Synchronization.Interfaces.Sinc;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Local;
using HomeNetOrm.Repositories.Local.Sync;

namespace HomeNet.Synchronization.Items
{
    public class MessageSyncItem : IEntitySyncItem, IDisposable
    {
        private readonly IMessageRepo _messageRepository;
        private readonly DeviceSettingsRepo _deviceSettingsRepo;
        private readonly IMessageApiClient _apiClient;
        private readonly ICastomLogger _logger;
        private readonly IEventBus _eventBus;
        private readonly SemaphoreSlim _pushLock = new(1, 1);

        public int Order => 2;

        public MessageSyncItem(
            IMessageRepo messageRepository,
            DeviceSettingsRepo deviceSettingsRepo,
            IMessageApiClient apiClient,
            ICastomLogger logger,
            IEventBus eventBus)
        {
            _messageRepository = messageRepository;
            _deviceSettingsRepo = deviceSettingsRepo;
            _apiClient = apiClient;
            _logger = logger;
            _eventBus = eventBus;

            // Мгновенно толкаем очередь при каждом новом сохранении в локальный SQLite
            _eventBus.SubscribeAsync<IMessageRepo.Saved>(OnLocalMessageSaved);
        }

        private Task OnLocalMessageSaved(IMessageRepo.Saved _) => PushAsync();

        public async Task PullAsync(CancellationToken ct = default)
        {
            try
            {
                var session = await _deviceSettingsRepo.GetActiveSessionAsync();
                if (session == null || session.UserId == Guid.Empty) return;

                var batch = await _apiClient.FetchFreshMessagesAsync(session.LastMessageVersion);

                await _messageRepository.ApplySyncBatchAsync(batch.UpdatedItems);

                if (batch.LatestVersion > session.LastMessageVersion)
                {
                    await _deviceSettingsRepo.UpdateLastMessageVersionAsync(session.UserId, batch.LatestVersion);
                    _logger.LogInfo($"[MessageSyncItem] Курсор сообщений обновлён: {session.LastMessageVersion} -> {batch.LatestVersion}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[MessageSyncItem] Ошибка PULL: {ex.Message}");
            }
        }

        public async Task PushAsync(CancellationToken ct = default)
        {
            // Защита от одновременного запуска нескольких PushAsync, если юзер строчит сообщения одно за другим
            if (!await _pushLock.WaitAsync(0, ct))
                return;

            try
            {
                var pending = await _messageRepository.GetPendingMessagesAsync();
                foreach (var message in pending)
                {
                    await _apiClient.PushMessageAsync(message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[MessageSyncItem] Ошибка PUSH: {ex.Message}");
            }
            finally
            {
                _pushLock.Release();
            }
        }

        public void Dispose()
        {
            _eventBus.UnsubscribeAsync<IMessageRepo.Saved>(OnLocalMessageSaved);
            _pushLock.Dispose();
        }
    }
}
