using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.Handlers;
using Microsoft.AspNetCore.SignalR.Client;

namespace HomeNet.Network.SignalRListeners
{
    // MessageHubHandler.cs
    public class MessageHubHandler(ICastomLogger logger, IEventBus eventBus) : IHubHandler
    {
        public void Register(HubConnection connection)
        {
            connection.On<MessageEntity>("ReceiveMessage", (message) =>
            {
                logger.LogInfo($"[СЕТЬ] Поймали сообщение {message.Id} от сервера.");
                message.IsSynced = 1;
                eventBus.Publish(this, new MessageReceivedFromNetwork(message));
                return Task.CompletedTask;
            });
        }
    }

    public record MessageReceivedFromNetwork(MessageEntity Message);
}
