using HomeNet.Infrastructure.Shared;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Network.SignalR;
using HomeNetCore.Interfaces.ViewModels.UserZone;
using Microsoft.AspNetCore.SignalR.Client;
using static HomeNetCore.Interfaces.ViewModels.UserZone.IDeviceSettingsVM;

namespace HomeNet.Network.SignalRListeners
{
    public class SignalRHubClient : IAsyncDisposable
    {
        private readonly string _apiBaseUrl;
        private readonly ICastomLogger _logger;
        private readonly IEventBus _eventBus;
        private readonly IEnumerable<IHubHandler> _handlers;
        private HubConnection? _connection;

        public SignalRHubClient(
            string apiBaseUrl,
            ICastomLogger logger,
            IEventBus eventBus,
            IEnumerable<IHubHandler> handlers)
        {
            _apiBaseUrl = apiBaseUrl;
            _logger = logger;
            _eventBus = eventBus;
            _handlers = handlers;

            _eventBus.Subscribe<SessionPersisted>(OnSessionPersisted);
            _eventBus.Subscribe<IUserVm.LogoutClient>(OnLogoutClient);
        }

        private async void OnSessionPersisted(SessionPersisted msg)
        {
            if (msg.UserId != Guid.Empty)
            {
                _logger.LogInfo($"[СЕТЬ] Сессия готова ({msg.UserId}). Поднимаем единый SignalR...");
                await ConnectAsync(msg.UserId);
            }
        }

        private async void OnLogoutClient(IUserVm.LogoutClient msg)
        {
            await DisconnectAsync();
        }

        public async Task ConnectAsync(Guid? userId = null)
        {
            await DisconnectAsync();

            string url = userId.HasValue
                ? $"{_apiBaseUrl}/chatHub?userId={userId.Value}"
                : $"{_apiBaseUrl}/chatHub";

            _connection = new HubConnectionBuilder()
                .WithUrl(url)
                .WithAutomaticReconnect()
                .Build();

            // Подключаем все обработчики (сообщения, юзеры, логи) к ОДНОМУ сокету
            foreach (var handler in _handlers)
            {
                handler.Register(_connection);
            }

            // Сигнал готовности от сервера теперь ловится строго ОДИН раз
            _connection.On("ReceiveServerReady", () =>
            {
                _logger.LogInfo("[СЕТЬ] 🔥 Получен сигнал готовности от сервера!");
                _eventBus.Publish(this, new ServerBecameReadyEvent());
                return Task.CompletedTask;
            });

            await AutoExceptionHandler.ExecuteSafeAsync(_logger, async () =>
            {
                await _connection.StartAsync();
                return true;
            });

            if (_connection.State == HubConnectionState.Connected)
                _logger.LogInfo("[СЕТЬ] Единый SignalR-хаб успешно поднят.");
            else
                _logger.LogWarning("[СЕТЬ] SignalR не подключен. Работаем в офлайн-режиме.");
        }

        private async Task DisconnectAsync()
        {
            if (_connection != null)
            {
                _logger.LogInfo("[СЕТЬ] Закрываем единое соединение SignalR...");
                await _connection.DisposeAsync();
                _connection = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            _eventBus.Unsubscribe<SessionPersisted>(OnSessionPersisted);
            _eventBus.Unsubscribe<IUserVm.LogoutClient>(OnLogoutClient);
            await DisconnectAsync();
        }
    }

    public record ServerBecameReadyEvent();
}
