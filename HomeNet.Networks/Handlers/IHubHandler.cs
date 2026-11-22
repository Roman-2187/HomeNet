using Microsoft.AspNetCore.SignalR.Client;

namespace HomeNet.Network.Handlers
{
    public interface IHubHandler
    {
        void Register(HubConnection connection);
    }
}
