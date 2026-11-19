namespace HomeNet.Network.Api
{
    public interface IApiClient
    {
        Task PingAsync(CancellationToken cancellationToken = default);
        void SetSessionHeader(Guid sessionId);
        void ClearSessionHeader();
    }
}
