using HomeNetCore.Interfaces.Services;
using HomeNetOrm.Repositories.Local;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;

namespace HomeNetServices.Identity
{
    public class DeviceSettingsService : IDeviceSettingsService
    {
        private readonly ClientDeviceSettingsRepository _deviceSettingsRepo;
        private readonly ICastomLogger _logger;

        private DeviceSettingsEntity? _cachedActiveSession;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public DeviceSettingsService(ClientDeviceSettingsRepository deviceSettingsRepo, ICastomLogger logger)
        {
            _deviceSettingsRepo = deviceSettingsRepo ?? throw new ArgumentNullException(nameof(deviceSettingsRepo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private async Task EnsureActiveSessionLoadedAsync()
        {
            if (_cachedActiveSession != null) return;

            await _semaphore.WaitAsync();
            try
            {
                if (_cachedActiveSession == null)
                {
                    Guid deviceId = ImmortalDeviceProvider.CurrentDeviceId;
                    _cachedActiveSession = await _deviceSettingsRepo.GetActiveSessionAsync();
                    int totalAccounts = await _deviceSettingsRepo.GetKnownAccountsCountAsync();

                    _logger.LogInfo($"[DeviceSettingsService] Железо ID: {deviceId} | Аккаунтов в истории: {totalAccounts} | Активен: {_cachedActiveSession?.Email ?? "никто"}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[DeviceSettingsService] Ошибка прогрева сессии: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public Task<Guid> GetDeviceIdAsync()
        {
            return Task.FromResult(ImmortalDeviceProvider.CurrentDeviceId);
        }

        public async Task<DeviceSettingsEntity?> GetCurrentSettingsAsync()
        {
            await EnsureActiveSessionLoadedAsync();
            return _cachedActiveSession;
        }

        public async Task SaveSessionAsync(Guid userId, string email, string password)
        {
            try
            {
                Guid deviceId = ImmortalDeviceProvider.CurrentDeviceId;
                Guid pairSessionId = ImmortalDeviceProvider.CreatePairSessionId(deviceId, userId);

                // Проверяем, заходил ли этот юзер раньше с этого ноутбука, чтобы не потерять его LastSyncVersion!
                var existing = await _deviceSettingsRepo.GetBySessionIdAsync(pairSessionId);

                var settings = existing ?? new DeviceSettingsEntity
                {
                    Id = pairSessionId,
                    DeviceId = deviceId,
                    UserId = userId,
                    LastSyncVersion = 0
                };

                settings.Email = email;
                settings.Password = password;
                settings.IsActive = true;
                settings.UpdatedAt = DateTimeOffset.UtcNow;

                await _deviceSettingsRepo.SaveSettingsAsync(settings);
                _cachedActiveSession = settings;

                _logger.LogInfo($"[DeviceSettings] Активна сессия {email} (PairId: {pairSessionId}, Курсор версии: {settings.LastSyncVersion}).");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[DeviceSettings] Сбой сохранения сессии: {ex.Message}");
            }
        }

        public async Task ClearSessionAsync()
        {
            try
            {
                await EnsureActiveSessionLoadedAsync();

                var settings = _cachedActiveSession;
                if (settings == null) return;

                // Затираем ТОЛЬКО чувствительные данные (логин/пароль) и снимаем флаг активности.
                // UserId и LastSyncVersion остаются в SQLite для быстрого возврата в аккаунт!
                settings.Email = string.Empty;
                settings.Password = string.Empty;
                settings.IsActive = false;
                settings.UpdatedAt = DateTimeOffset.UtcNow;

                await _deviceSettingsRepo.SaveSettingsAsync(settings);
                _cachedActiveSession = null;

                _logger.LogInfo($"[DeviceSettings] Креды стерты, но история аккаунта (UserId: {settings.UserId}) и курсор версии сохранены.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[DeviceSettings] Ошибка очистки сессии: {ex.Message}");
            }
        }
    }
}
