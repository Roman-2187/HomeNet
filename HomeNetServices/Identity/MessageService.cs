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

            MessageEntity entity = new MessageEntity
            {
                Id = Guid.NewGuid(),
                SenderId = senderId,
                ReceiverId = receiverId,
                Text = text.Trim(),
                MediaType = "Text",
                IsRead = false,
                LocalCreatedAt = DateTime.UtcNow,
                IsSynced = 1 // Улетает на сервер
            };

            try
            {
                // 🔥 Отправляем через API (API опубликует факт, SyncService запишет в SQLite)
                bool success = await _apiClient.PushMessageAsync(entity);

                return success
                    ? new ChatVerdict(true, string.Empty, entity) 
                    : new ChatVerdict(false, "Сервер недоступен. Сообщение не отправлено.", null);
            }
            catch (Exception ex)
            {
                return new ChatVerdict(false, $"Ошибка API: {ex.Message}", null);
            }
        }

        public async Task<IEnumerable<MessageEntity>> GetPrivateHistoryAsync(Guid userId, Guid friendId)
        {
          
                IEnumerable<MessageEntity> allMessages = await _messageRepo.GetChatHistoryAsync(userId, friendId);
                return allMessages.OrderBy(m => m.LocalCreatedAt);
            
           
        }
    }
}