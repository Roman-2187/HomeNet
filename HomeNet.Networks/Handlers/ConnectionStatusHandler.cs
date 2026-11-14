using HomeNet.Infrastructure.Shared.Interfaces.Routing;

namespace HomeNet.Network.Handlers
{
    public class ConnectionStatusHandler : DelegatingHandler
    {
        private readonly IEventBus _eventBus;
        private bool? _lastKnownState; // null при старте, чтобы первый запрос точно обновил UI

        public ConnectionStatusHandler(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Пропускаем запрос дальше к серверу
                var response = await base.SendAsync(request, cancellationToken);

                // Если сервер ответил — значит связь есть!
                NotifyStateChanged(isOnline: true);

                return response;
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                // Упала сеть или вышел таймаут — сервер недоступен
                NotifyStateChanged(isOnline: false);
                throw; // Пробрасываем ошибку дальше, чтобы ApiClient её залогировал
            }
        }

        private void NotifyStateChanged(bool isOnline)
        {
            if (_lastKnownState == isOnline) return;

            _lastKnownState = isOnline;
            _eventBus.Publish(this, new ServerStatusChangedEvent(isOnline));
        }

        public record ServerStatusChangedEvent(bool IsOnline);
    }





}
