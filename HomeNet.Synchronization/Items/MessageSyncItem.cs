using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Synchronization.Interfaces.Sinc;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Local;

namespace HomeNet.Synchronization.Items
{
    public class MessageSyncItem : IEntitySyncItem
    {
        private readonly IMessageRepo _messageRepository;
        private readonly ClientDeviceSettingsRepository _deviceSettingsRepo;
        private readonly IMessageApiClient _apiClient;
        private readonly ICastomLogger _logger;

        public int Order => 2;

        public MessageSyncItem(
            IMessageRepo messageRepository,
            ClientDeviceSettingsRepository deviceSettingsRepo,
            IMessageApiClient apiClient,
            ICastomLogger logger)
        {
            _messageRepository = messageRepository;
            _deviceSettingsRepo = deviceSettingsRepo;
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task PullAsync(CancellationToken ct = default)
        {
            try
            {
                var session = await _deviceSettingsRepo.GetActiveSessionAsync();
                if (session == null || session.UserId == Guid.Empty) return;

                _logger.LogInfo($"[MessageSyncItem] PULL сообщений для {session.UserId}...");
                var freshMessages = await _apiClient.FetchFreshMessagesAsync(session.UserId, DateTimeOffset.MinValue);

                foreach (var message in freshMessages)
                {
                    message.IsSynced = 1;
                    try
                    {
                        await _messageRepository.InsertAsync(message);
                    }
                    catch (DuplicateMessageException)
                    {
                        // Дубликаты пропускаем
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[MessageSyncItem] Ошибка PULL: {ex.Message}");
            }
        }

        public async Task PushAsync(CancellationToken ct = default)
        {
            try
            {
                var allMessages = await _messageRepository.GetAllAsync();
                var pending = allMessages.Where(m => m.IsSynced == 0).ToList();

                foreach (var message in pending)
                {
                    await _apiClient.PushMessageAsync(message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[MessageSyncItem] Ошибка PUSH: {ex.Message}");
            }
        }
    }
}
