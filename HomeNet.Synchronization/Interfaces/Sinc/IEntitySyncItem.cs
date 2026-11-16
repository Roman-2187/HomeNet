namespace HomeNet.Synchronization.Interfaces.Sinc
{
    public interface IEntitySyncItem
    {
        int Order { get; }
        Task PullAsync(CancellationToken ct = default);
        Task PushAsync(CancellationToken ct = default);
    }
}
