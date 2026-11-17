// UserHubHandler.cs
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace HomeNet.Network.SignalRListeners
{
    public class UserHubHandler(ICastomLogger logger, IEventBus eventBus) : IHubHandler
    {
        public void Register(HubConnection connection)
        {
            connection.On<UserEntity>("ReceiveUserSync", (user) =>
            {
                logger.LogInfo($"[СЕТЬ] Поймали юзера {user.Id} от сервера.");
                user.IsSynced = 1;
                eventBus.Publish(this, new UserReceivedFromNetwork(user));
                return Task.CompletedTask;
            });

            connection.On<Guid>("ReceiveUserDeleted", (userId) =>
            {
                logger.LogWarning($"[СЕТЬ] Поймали удаление юзера {userId}.");
                eventBus.Publish(this, new UserDeletedFromNetwork(userId));
                return Task.CompletedTask;
            });
        }
    }

    public record UserReceivedFromNetwork(UserEntity User);
    public record UserDeletedFromNetwork(Guid UserId);
}