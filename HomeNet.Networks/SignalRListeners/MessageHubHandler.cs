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

            // 🆕 Ловим мягкое удаление сообщения от собеседника
            connection.On<Guid>("MessageDeleted", (messageId) =>
            {
                logger.LogInfo($"[СЕТЬ] Сервер сообщил об удалении сообщения {messageId}.");
                eventBus.Publish(this, new MessageDeletedFromNetwork(messageId));
                return Task.CompletedTask;
            });
        }
    }

    public record MessageReceivedFromNetwork(MessageEntity Message);
    public record MessageDeletedFromNetwork(Guid MessageId); // 🆕 Событие в шину
}
