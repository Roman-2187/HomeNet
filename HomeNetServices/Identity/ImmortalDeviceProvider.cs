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

        private static readonly Lazy<Guid> _cachedDeviceId = new(LoadOrCreateDeviceId);

        /// <summary>
        /// Постоянный GUID текущего устройства (выживает даже при сносе локального SQLite).
        /// </summary>
        public static Guid CurrentDeviceId => _cachedDeviceId.Value;

        /// <summary>
        /// Генерирует детерминированный GUID для пары (DeviceId + UserId) через MD5.
        /// Для одного и того же юзера на этом железе результат всегда идентичен.
        /// </summary>
        public static Guid CreatePairSessionId(Guid deviceId, Guid userId)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes($"{deviceId:N}:{userId:N}");
            byte[] hashBytes = MD5.HashData(inputBytes);
            return new Guid(hashBytes);
        }

        private static Guid LoadOrCreateDeviceId()
        {
            try
            {
                if (File.Exists(IdentityFilePath))
                {
                    var json = File.ReadAllText(IdentityFilePath);
                    var identity = (DeviceIdentity?)JsonSerializer.Deserialize(json, typeof(DeviceIdentity));
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
