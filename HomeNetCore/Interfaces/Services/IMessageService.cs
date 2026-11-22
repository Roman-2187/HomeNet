using HomeNet.Models.Shared.Entity;

namespace HomeNetCore.Interfaces.Services
{
    public interface IMessageService
    {
        // Вердикт по аналогии с IRegistrationService.Verdict
        public record ChatVerdict(bool IsValid, string ErrorMessage, MessageEntity? CreatedMessage);

        Task<ChatVerdict> SendPrivateMessageAsync(Guid senderId, Guid receiverId, string text);

        /// <summary>
        /// Вытаскивает историю зашифрованной переписки между двумя пользователями из СУБД.
        /// </summary>
        Task<IEnumerable<MessageEntity>> GetPrivateHistoryAsync(Guid userId, Guid friendId);
    }
}