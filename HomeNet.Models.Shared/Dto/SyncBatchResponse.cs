namespace HomeNet.Models.Shared.Dto
{
    public class SyncBatchResponse<T>
    {
        public List<T> UpdatedItems { get; set; } = new();
        public List<Guid> DeletedIds { get; set; } = new();
        public long LatestVersion { get; set; }
    }
}
