// BaseApiClient.cs
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Network.Api;

namespace HomeNet.Network.ApiClient
{
   

    public abstract class BaseApiClient(HttpClient httpClient, ICastomLogger logger) : IApiClient
    {
        protected readonly HttpClient HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        protected readonly ICastomLogger Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public void SetSessionHeader(Guid sessionId)
        {
            HttpClient.DefaultRequestHeaders.Remove("X-Session-Id");
            if (sessionId != Guid.Empty)
            {
                HttpClient.DefaultRequestHeaders.Add("X-Session-Id", sessionId.ToString());
            }
        }

        public void ClearSessionHeader()
        {
            HttpClient.DefaultRequestHeaders.Remove("X-Session-Id");
        }

        public async Task PingAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(3));

                var response = await HttpClient.GetAsync("api/health", cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(cts.Token);
                    Logger.LogInfo($"[Ping] Ответ сервера: {json}");
                }
                else
                {
                    Logger.LogWarning($"[Ping] Сервер ответил с ошибкой: {(int)response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[Ping] Сервер недоступен: {ex.Message}");
            }
        }
    }
}
