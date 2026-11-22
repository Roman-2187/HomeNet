using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HomeNetServices.Identity
{
    public static class ImmortalDeviceProvider
    {
        private record DeviceIdentity(Guid DeviceId, DateTimeOffset CreatedAt);

        private static readonly string IdentityFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HomeNet",
            "device_identity.json");

        private static readonly Lazy<Guid> CachedDeviceId = new(LoadOrCreateDeviceId);

        /// <summary>
        /// Постоянный GUID текущего устройства (выживает даже при сносе локального SQLite).
        /// </summary>
        public static Guid CurrentDeviceId => CachedDeviceId.Value;

        /// <summary>
        /// Генерирует детерминированный SessionId для указанного пользователя на текущем устройстве.
        /// </summary>
        public static Guid CreateSessionIdForCurrentDevice(Guid userId)
        {
            return CreatePairSessionId(CurrentDeviceId, userId);
        }

        /// <summary>
        /// Генерирует детерминированный GUID для пары (DeviceId + UserId) через MD5.
        /// Для одного и того же юзера на этом железе результат всегда идентичен.
        /// </summary>
        public static Guid CreatePairSessionId(Guid deviceId, Guid userId)
        {
            var inputBytes = Encoding.UTF8.GetBytes($"{deviceId:N}:{userId:N}");
            var hashBytes = MD5.HashData(inputBytes);
            return new Guid(hashBytes);
        }

        private static Guid LoadOrCreateDeviceId()
        {
            try
            {
                if (File.Exists(IdentityFilePath))
                {
                    var json = File.ReadAllText(IdentityFilePath);
                    var identity = JsonSerializer.Deserialize<DeviceIdentity>(json);
                    if (identity != null && identity.DeviceId != Guid.Empty)
                    {
                        return identity.DeviceId;
                    }
                }
            }
            catch
            {
                // Если файл повреждён — пересоздадим ниже
            }

            var newIdentity = new DeviceIdentity(Guid.NewGuid(), DateTimeOffset.UtcNow);
            var directory = Path.GetDirectoryName(IdentityFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var serialized = JsonSerializer.Serialize(newIdentity, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(IdentityFilePath, serialized);

            return newIdentity.DeviceId;
        }
    }
}
