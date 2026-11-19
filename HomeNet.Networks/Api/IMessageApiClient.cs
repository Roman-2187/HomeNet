using HomeNet.Models.Shared.Dto;
using HomeNet.Models.Shared.Entity;

namespace HomeNet.Network.Api
{
    public interface IMessageApiClient : IApiClient
    {
        Task<bool> PushMessageAsync(MessageEntity message);

        Task<SyncBatchResponse<MessageEntity>> FetchFreshMessagesAsync(long sinceVersion);



        // 🔥 ФАКТЫ (публикует сам ApiClient)
        public record MessageSentOnServer(MessageEntity Message);
        public record MessageSendFailed(Guid MessageId, string Error);
    }
}