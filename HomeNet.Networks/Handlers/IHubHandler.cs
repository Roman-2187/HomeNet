using Microsoft.AspNetCore.SignalR.Client;

namespace HomeNet.Network.SignalR
{
    public interface IHubHandler
    {
        void Register(HubConnection connection);
    }
}
