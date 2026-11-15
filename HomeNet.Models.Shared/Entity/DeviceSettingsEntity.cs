namespace HomeNet.Models.Shared.Entity
{
    /// <summary>
    /// Внутреннее хранилище сессий и курсоров синхронизации (SQLite).
    /// Хранит историю всех аккаунтов, заходивших с этого устройства.
    /// </summary>
    public class DeviceSettingsEntity
    {
        // 🔐 ДЕТЕРМИНИРОВАННЫЙ ПЕРВИЧНЫЙ КЛЮЧ (MD5 от DeviceId + UserId)
        public Guid Id { get; set; }

        // 💻 Бессмертный ID железа
        public Guid DeviceId { get; set; }

        // 👤 ID пользователя (не стирается при выходе!)
        public Guid UserId { get; set; }

        // ==========================================
        // 🔄 ПЕРЕПИСЫВАЕМЫЕ ПОЛЯ (ДЛЯ АВТОВХОДА)
        // ==========================================
        public string? Email { get; set; }
        public string? Password { get; set; }
        public bool IsActive { get; set; }

        // 📈 Закладка синхронизации для этого юзера на этом устройстве
        public long LastSyncVersion { get; set; }

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}


