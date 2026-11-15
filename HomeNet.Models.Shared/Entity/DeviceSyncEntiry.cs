namespace HomeNet.Models.Shared.Entity
{
    public class DeviceSyncEntity
    {
        public Guid Id { get; set; } // Детерминированный Primary Key (MD5 от DeviceId + UserId)
        public Guid DeviceId { get; set; }
        public Guid UserId { get; set; }
        public long LastVersion { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
