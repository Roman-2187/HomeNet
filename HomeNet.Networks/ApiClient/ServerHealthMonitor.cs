// ServerHealthMonitor.cs
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetOrm.Interfaces.Builders;

namespace HomeNet.Network.ApiClient
{
    public class ServerHealthMonitor : BaseApiClient, IDisposable
    {
        private readonly IEventBus _eventBus;

        public ServerHealthMonitor(HttpClient httpClient, ICastomLogger logger, IEventBus eventBus)
            : base(httpClient, logger)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _eventBus.Subscribe<IDbContextContainer.InfrastructureReady>(OnInfrastructureReady);
        }

        private async void OnInfrastructureReady(IDbContextContainer.InfrastructureReady msg)
        {
            try
            {
                Logger.LogInfo("[Сеть] Локальная БД готова. Выполняю стартовую проверку сервера...");
                await PingAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[Сеть] Ошибка при стартовой проверке: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<IDbContextContainer.InfrastructureReady>(OnInfrastructureReady);
        }
    }
}
