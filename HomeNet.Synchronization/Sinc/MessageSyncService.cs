using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.SignalRListeners;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Local.Sync;
using static HomeNet.Network.Api.IMessageApiClient;

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
            _eventBus.SubscribeAsync<MessageSentOnServer>(OnMessageSent);
            _eventBus.SubscribeAsync<MessageReceivedFromNetwork>(OnMessageReceivedFromNetwork);
    
            // 🆕 Ловим подтверждение удаления от нашего API
            _eventBus.SubscribeAsync<MessageDeletedOnServer>(OnMessageDeletedOnServer);
    
            // 🆕 Ловим удаление от собеседника через SignalR (подставь имя своего события из SignalRListeners)
            _eventBus.SubscribeAsync<MessageDeletedFromNetwork>(OnMessageDeletedFromNetwork);
        }
        
        
        private async Task OnMessageSent(MessageSentOnServer msg)
        {
            _logger.LogInfo($"[MessageRealtime] Сервер подтвердил отправку {msg.Message.Id}. Ставим IsSynced = 1.");
            try
            {
                msg.Message.IsSynced = 1;
                await _messageRepository.UpdateAsync(msg.Message); // или отдельный метод MarkAsSyncedAsync(msg.Message.Id)
            }
            catch (Exception ex)
            {
                _logger.LogError($"[MessageRealtime] Ошибка обновления статуса синка {msg.Message.Id}: {ex.Message}");
            }
        }

        
        
        private async Task OnMessageDeletedOnServer(MessageDeletedOnServer msg)
        {
            _logger.LogInfo($"[MessageRealtime] Сервер подтвердил удаление {msg.MessageId}. Затираем в SQLite.");
            await TrySoftDeleteAsync(msg.MessageId);
        }
        
        
        private async Task OnMessageReceivedFromNetwork(MessageReceivedFromNetwork msg)
        {
            _logger.LogInfo($"[MessageRealtime] Сеть прислала сообщение {msg.Message.Id}. Пишем в SQLite.");
            await TryUpsertMessageAsync(msg.Message);
        }


        private async Task OnMessageDeletedFromNetwork(MessageDeletedFromNetwork msg)
        {
            _logger.LogInfo($"[MessageRealtime] Собеседник удалил сообщение {msg.MessageId}. Затираем в SQLite.");
            await TrySoftDeleteAsync(msg.MessageId);
        }
        
        
        
        
        
        private async Task TryUpsertMessageAsync(MessageEntity message)
        {
            try
            {
                message.IsSynced = 1;
                await _messageRepository.SaveOrSyncMessageAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[MessageRealtime] Не удалось сохранить/обновить сообщение {message.Id}: {ex.Message}");
            }
        }


        
        
        private async Task TrySoftDeleteAsync(Guid messageId)
        {
            try
            {
                await _messageRepository.SoftDeleteAsync(messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[MessageRealtime] Не удалось мягко удалить сообщение {messageId}: {ex.Message}");
            }
        }
        
        
        
        public void Dispose()
        {
            _eventBus.UnsubscribeAsync<MessageSentOnServer>(OnMessageSent);
            _eventBus.UnsubscribeAsync<MessageReceivedFromNetwork>(OnMessageReceivedFromNetwork);
        }
    }
}
