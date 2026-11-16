using HomeNet.Infrastructure.Shared;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Interfaces.ViewModels.UserZone;
using Microsoft.AspNetCore.SignalR.Client;
using static HomeNetCore.Interfaces.ViewModels.UserZone.IDeviceSettingsVM;

namespace HomeNet.Network.SignalRListeners
{
    public abstract class BaseSignalRListener : IAsyncDisposable
    {
        protected readonly string ApiBaseUrl;
        protected readonly ICastomLogger Logger;
        protected readonly IEventBus EventBus;
        protected HubConnection? Connection;

        protected BaseSignalRListener(string apiBaseUrl, ICastomLogger logger, IEventBus eventBus)
        {
            ApiBaseUrl = apiBaseUrl;
            Logger = logger;
            EventBus = eventBus;

            // Ждем то же событие готовности сессии, что и SyncBatchFactory
            EventBus.Subscribe<SessionPersisted>(OnSessionPersisted);
            EventBus.Subscribe<IUserVm.LogoutClient>(OnLogoutClient);
        }

        private async void OnSessionPersisted(SessionPersisted msg)
        {
            if (msg.UserId != Guid.Empty)
            {
                Logger.LogInfo($"[СЕТЬ] Сессия готова ({msg.UserId}). Поднимаем SignalR для {GetType().Name}...");
                await ConnectAsync(msg.UserId);
            }
        }

        private async void OnLogoutClient(IUserVm.LogoutClient msg)
        {
            if (Connection != null)
            {
                Logger.LogInfo($"[СЕТЬ] Логаут. Закрываем SignalR для {GetType().Name}...");
                await Connection.DisposeAsync();
                Connection = null;
            }
        }

        public async Task ConnectAsync(Guid? userId = null)
        {
            if (Connection != null)
            {
                await Connection.DisposeAsync();
                Connection = null;
            }

            string url = userId.HasValue
                ? $"{ApiBaseUrl}/chatHub?userId={userId.Value}"
                : $"{ApiBaseUrl}/chatHub";

            Connection = new HubConnectionBuilder()
                .WithUrl(url)
                .WithAutomaticReconnect()
                .Build();

            RegisterHandlers(Connection);

            Connection.On("ReceiveServerReady", () =>
            {
                Logger.LogInfo("[СЕТЬ] 🔥 Получен сигнал готовности от сервера!");
                EventBus.Publish(this, new ServerBecameReadyEvent());
                return Task.CompletedTask;
            });

            await AutoExceptionHandler.ExecuteSafeAsync(Logger, async () =>
            {
                await Connection.StartAsync();
                return true;
            });

            if (Connection.State == HubConnectionState.Connected)
            {
                Logger.LogInfo($"[СЕТЬ] SignalR поднят для {GetType().Name}.");
            }
            else
            {
                Logger.LogWarning($"[СЕТЬ] SignalR не подключен для {GetType().Name}. Работаем в офлайн-режиме.");
            }
        }

        protected abstract void RegisterHandlers(HubConnection connection);

        public async ValueTask DisposeAsync()
        {
            EventBus.Unsubscribe<SessionPersisted>(OnSessionPersisted);
            EventBus.Unsubscribe<IUserVm.LogoutClient>(OnLogoutClient);

            if (Connection != null)
            {
                await Connection.DisposeAsync();
                Connection = null;
            }
        }
    }

    public record ServerBecameReadyEvent();
}

