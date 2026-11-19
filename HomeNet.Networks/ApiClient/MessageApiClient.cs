using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Dto;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.Api;
using HomeNet.Network.ApiClient;
using System.Net.Http.Json;

public class MessageApiClient : BaseApiClient, IMessageApiClient
{
    private readonly IEventBus _eventBus;

    public MessageApiClient(HttpClient httpClient, ICastomLogger logger, IEventBus eventBus)
        : base(httpClient, logger)
    {
        _eventBus = eventBus;
    }

    public async Task<bool> PushMessageAsync(MessageEntity message)
    {
        try
        {
            var response = await HttpClient.PostAsJsonAsync("/api/messages", message);

            if (response.IsSuccessStatusCode)
            {
                _eventBus.Publish(this, new IMessageApiClient.MessageSentOnServer(message));
                return true;
            }

            _eventBus.Publish(this, new IMessageApiClient.MessageSendFailed(message.Id, $"HTTP {response.StatusCode}"));
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError($"[MessageApiClient] Ошибка отправки: {ex.Message}");
            _eventBus.Publish(this, new IMessageApiClient.MessageSendFailed(message.Id, ex.Message));
            return false;
        }
    }

    public async Task<SyncBatchResponse<MessageEntity>> FetchFreshMessagesAsync(long sinceVersion)
    {
        try
        {
            Logger.LogInfo($"[MessageApiClient] Запрос обновлений сообщений с версии {sinceVersion}...");
            var response = await HttpClient.GetFromJsonAsync<SyncBatchResponse<MessageEntity>>($"/api/messages/sync?sinceVersion={sinceVersion}");
            return response ?? new SyncBatchResponse<MessageEntity>();
        }
        catch (Exception ex)
        {
            Logger.LogError($"[MessageApiClient] Не удалось скачать обновления сообщений: {ex.Message}");
            return new SyncBatchResponse<MessageEntity>();
        }
    }

}
