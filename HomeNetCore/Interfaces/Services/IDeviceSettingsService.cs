using HomeNet.Models.Shared.Entity;

namespace HomeNetCore.Interfaces.Services
{
    public interface IDeviceSettingsService
    {
        Task<Guid> GetDeviceIdAsync();
        Guid CreateSessionIdForUser(Guid userId);
        Task<SettingsEntity?> GetCurrentSettingsAsync();
        Task SaveSessionAsync(Guid sessionId, Guid userId, string email);
        Task ClearSessionAsync();
    }
}
