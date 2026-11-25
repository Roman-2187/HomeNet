using HomeNet.Models.Shared.Entity;
using HomeNet.Network.Api;
using HomeNetCore.Interfaces.Services;
using HomeNetOrm.Interfaces.Repositories;
using static HomeNetCore.Interfaces.Services.IMessageService;
namespace HomeNetServices.Identity
{
    public class MessageService(IMessageApiClient apiClient, IMessageRepo messageRepo) : IMessageService
    {
        private readonly IMessageApiClient _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        private readonly IMessageRepo _messageRepo = messageRepo ?? throw new ArgumentNullException(nameof(messageRepo)); 

        public async Task<ChatVerdict> SendPrivateMessageAsync(Guid senderId, Guid receiverId, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new ChatVerdict(false, "Сообщение не может быть пустым", null);

            var entity = new MessageEntity
            {
                Id = Guid.NewGuid(),
                SenderId = senderId,
                ReceiverId = receiverId,
                Text = text.Trim(),
                MediaType = "Text",
                IsRead = false,
                LocalCreatedAt = DateTime.UtcNow,
                IsSynced = 0 // <-- Помечаем как несинхронизированное (Unsynced)
            };

            try
            {
                // Сразу сохраняем в локальную SQLite (внутри сработает EventBus.Publish(Saved))
                var savedEntity = await _messageRepo.InsertAsync(entity);
                return new ChatVerdict(true, string.Empty, savedEntity);
            }
            catch (Exception ex)
            {
                return new ChatVerdict(false, $"Ошибка локальной БД: {ex.Message}", null);
            }
        }
        
        
        public async Task<ChatVerdict> DeleteMessageAsync(Guid messageId)
        {
            try
            {
                // 1. Сначала просим сервер удалить сообщение
                var apiSuccess = await _apiClient.DeleteMessageAsync(messageId);
        
                if (!apiSuccess)
                    return new ChatVerdict(false, "Не удалось удалить сообщение на сервере.", null);

                // 2. Если сервер подтвердил — затираем текст в локальной SQLite
                await _messageRepo.SoftDeleteAsync(messageId);
        
                return new ChatVerdict(true, string.Empty, null);
            }
            catch (Exception ex)
            {
                return new ChatVerdict(false, $"Ошибка при удалении: {ex.Message}", null);
            }
        }



        public async Task<IEnumerable<MessageEntity>> GetPrivateHistoryAsync(Guid userId, Guid friendId)
        {
                var allMessages = await _messageRepo.GetChatHistoryAsync(userId, friendId);
                return allMessages.OrderBy(m => m.LocalCreatedAt);
        }
        
    }
}