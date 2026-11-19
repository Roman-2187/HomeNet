using HomeNet.Models.Shared.Dto;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.ApiClient;

namespace HomeNet.Network.Api
{
    public interface IMessageApiClient : IApiClientBase
    {
        Task<bool> PushMessageAsync(MessageEntity message);

        Task<SyncBatchResponse<MessageEntity>> FetchFreshMessagesAsync(long sinceVersion);



        // 🔥 ФАКТЫ (публикует сам ApiClient)
        public record MessageSentOnServer(MessageEntity Message);
        public record MessageSendFailed(Guid MessageId, string Error);
    }
}