using HomeNet.Models.Shared.Dto;
using HomeNet.Models.Shared.Entity;

namespace HomeNet.Network.Api
{
    public interface IMessageApiClient : IApiClient
    {
        Task<bool> PushMessageAsync(MessageEntity message);

        Task<SyncBatchResponse<MessageEntity>> FetchFreshMessagesAsync(long sinceVersion);

        public record MessageDeletedOnServer(Guid MessageId);
        public record MessageDeleteFailed(Guid MessageId, string Reason);

        Task<bool> DeleteMessageAsync(Guid messageId);

        // 🔥 ФАКТЫ (публикует сам ApiClient)
        public record MessageSentOnServer(MessageEntity Message);
        public record MessageSendFailed(Guid MessageId, string Error);
    }
}