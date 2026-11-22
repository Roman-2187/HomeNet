namespace HomeNet.Models.Shared.Dto
{
    public class SyncBatchResponse<T>
    {
        public List<T> UpdatedItems { get; set; } = [];
        public List<Guid> DeletedIds { get; set; } = [];
        public long LatestVersion { get; set; }
    }
}
