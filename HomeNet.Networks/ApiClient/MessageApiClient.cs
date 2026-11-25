using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Dto;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.Api;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using static HomeNet.Network.Api.IMessageApiClient;

namespace HomeNet.Network.ApiClient
{
    [SuppressMessage("ReSharper", "MethodHasAsyncOverload")]
    public class MessageApiClient(HttpClient httpClient, ICastomLogger logger, IEventBus eventBus)
        : BaseApiClient(httpClient, logger), IMessageApiClient
    {
        public async Task<bool> PushMessageAsync(MessageEntity message)
        {
            try
            {
                var response = await HttpClient.PostAsJsonAsync("/api/messages", message);

                if (response.IsSuccessStatusCode)
                {
                    eventBus.Publish(this, new MessageSentOnServer(message));
                    return true;
                }

                eventBus.Publish(this, new MessageSendFailed(message.Id, $"HTTP {response.StatusCode}"));
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[MessageApiClient] Ошибка отправки: {ex.Message}");
                eventBus.Publish(this, new MessageSendFailed(message.Id, ex.Message));
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
        
        
        
        public async Task<bool> DeleteMessageAsync(Guid messageId)
        {
            try
            {
                var response = await HttpClient.DeleteAsync($"/api/messages/{messageId}");

                if (response.IsSuccessStatusCode)
                {
                    eventBus.Publish(this, new MessageDeletedOnServer(messageId));
                    return true;
                }

                Logger.LogError($"[MessageApiClient] Сервер отклонил удаление {messageId}: HTTP {response.StatusCode}");
                eventBus.Publish(this, new MessageDeleteFailed(messageId, $"HTTP {response.StatusCode}"));
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[MessageApiClient] Ошибка удаления {messageId}: {ex.Message}");
                eventBus.Publish(this, new MessageDeleteFailed(messageId, ex.Message));
                return false;
            }
        }

        
        
        

    }
}