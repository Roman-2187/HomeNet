using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.SignalRListeners;
using HomeNetOrm.Interfaces.Repositories;

namespace HomeNet.Synchronization.Sinc
{
    public class MessageSyncService : IDisposable
    {
        private readonly IMessageRepo _messageRepository;
        private readonly IEventBus _eventBus;
        private readonly ICastomLogger _logger;

        public MessageSyncService(
            IMessageRepo messageRepository,
            IEventBus eventBus,
            ICastomLogger logger)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            _eventBus.Subscribe<IMessageApiClient.MessageSentOnServer>(OnMessageSent);
            _eventBus.Subscribe<MessageReceivedFromNetwork>(OnMessageReceivedFromNetwork);
        }

        private async void OnMessageSent(IMessageApiClient.MessageSentOnServer msg)
        {
            _logger.LogInfo($"[MessageRealtime] Сервер подтвердил сообщение {msg.Message.Id}. Пишем в SQLite.");
            await TryInsertMessageAsync(msg.Message);
        }

        private async void OnMessageReceivedFromNetwork(MessageReceivedFromNetwork msg)
        {
            _logger.LogInfo($"[MessageRealtime] Сеть прислала сообщение {msg.Message.Id}. Пишем в SQLite.");
            await TryInsertMessageAsync(msg.Message);
        }

        private async Task TryInsertMessageAsync(MessageEntity message)
        {
            try
            {
                message.IsSynced = 1;
                await _messageRepository.InsertAsync(message);
            }
            catch (DuplicateMessageException)
            {
                _logger.LogDebug($"[MessageRealtime] Сообщение {message.Id} пропущено по дубликату.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[MessageRealtime] Не удалось сохранить сообщение {message.Id}: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<IMessageApiClient.MessageSentOnServer>(OnMessageSent);
            _eventBus.Unsubscribe<MessageReceivedFromNetwork>(OnMessageReceivedFromNetwork);
        }
    }
}
