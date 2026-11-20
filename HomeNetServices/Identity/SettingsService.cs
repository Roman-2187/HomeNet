using HomeNetCore.Interfaces.Services;
using HomeNetOrm.Repositories.Local;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.Api;

namespace HomeNetServices.Identity
{
    public class SettingsService : IDeviceSettingsService
    {
        private readonly DeviceSettingsRepo _deviceSettingsRepo;
        private readonly IUserApiClient _apiClient;
        private readonly ICastomLogger _logger;

        private SettingsEntity? _cachedActiveSession;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public SettingsService(
            DeviceSettingsRepo deviceSettingsRepo,
            IUserApiClient apiClient,
            ICastomLogger logger)
        {
            _deviceSettingsRepo = deviceSettingsRepo ?? throw new ArgumentNullException(nameof(deviceSettingsRepo));
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
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

                    // Если нашли активную сессию в SQLite при старте — сразу ставим заголовок X-Session-Id для всех запросов
                    if (_cachedActiveSession != null && _cachedActiveSession.Id != Guid.Empty)
                    {
                        _apiClient.SetSessionHeader(_cachedActiveSession.Id);
                    }

                    _logger.LogInfo($"[SettingsService] Железо ID: {deviceId} | Активен: {_cachedActiveSession?.Email ?? "никто"}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[SettingsService] Ошибка прогрева сессии: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public Task<Guid> GetDeviceIdAsync() => Task.FromResult(ImmortalDeviceProvider.CurrentDeviceId);

        public Guid CreateSessionIdForUser(Guid userId) =>
            ImmortalDeviceProvider.CreateSessionIdForCurrentDevice(userId);

        public async Task<SettingsEntity?> GetCurrentSettingsAsync()
        {
            await EnsureActiveSessionLoadedAsync();
            return _cachedActiveSession;
        }

        public async Task SaveSessionAsync(Guid sessionId, Guid userId, string email)
        {
            try
            {
                Guid deviceId = ImmortalDeviceProvider.CurrentDeviceId;
                Guid activeSessionId = sessionId != Guid.Empty
                    ? sessionId
                    : ImmortalDeviceProvider.CreateSessionIdForCurrentDevice(userId);

                var existing = await _deviceSettingsRepo.GetBySessionIdAsync(activeSessionId);

                var settings = existing ?? new SettingsEntity
                {
                    Id = activeSessionId,
                    DeviceId = deviceId,
                    UserId = userId
                };

                settings.Email = email;
                settings.IsActive = true;
                settings.UpdatedAt = DateTimeOffset.UtcNow;

                await _deviceSettingsRepo.SaveSettingsAsync(settings);
                _cachedActiveSession = settings;

                // Устанавливаем заголовок X-Session-Id в общий HttpClient для всех последующих запросов
                _apiClient.SetSessionHeader(activeSessionId);

                _logger.LogInfo($"[SettingsService] Активна сессия {email} (SessionId: {activeSessionId}).");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[SettingsService] Сбой сохранения сессии: {ex.Message}");
            }
        }

        public async Task ClearSessionAsync()
        {
            try
            {
                await EnsureActiveSessionLoadedAsync();

                var settings = _cachedActiveSession;
                if (settings == null) return;

                // 1. Сначала просим сервер удалить текущую сессию (пока X-Session-Id в заголовке!)
                await _apiClient.LogoutAsync();

                // 2. Затем деактивируем локально в SQLite
                settings.IsActive = false;
                settings.UpdatedAt = DateTimeOffset.UtcNow;

                await _deviceSettingsRepo.SaveSettingsAsync(settings);
                _cachedActiveSession = null;

                // 3. И только теперь стираем заголовок авторизации
                _apiClient.ClearSessionHeader();

                _logger.LogInfo($"[SettingsService] Сессия для UserId: {settings.UserId} деактивирована.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[SettingsService] Ошибка очистки сессии: {ex.Message}");
            }
        }

    }
}
