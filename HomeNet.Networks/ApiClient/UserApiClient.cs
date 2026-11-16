using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Dto;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.Api;
using System.Net.Http.Json;

namespace HomeNet.Network.ApiClient
{
    public class UserApiClient(HttpClient httpClient, ICastomLogger logger, IEventBus eventBus) : BaseApiClient(httpClient, logger), IUserApiClient
    {
        private readonly IEventBus _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

        private class EmailCheckResponse
        {
            public bool Exists { get; set; }
        }

        public async Task<bool> PushUserAsync(UserEntity user)
        {
            try
            {
                var response = await HttpClient.PostAsJsonAsync("/api/users", user);

                if (response.IsSuccessStatusCode)
                {
                    // 🔥 Кричим, что сервер принял
                    _eventBus.Publish(this, new IUserApiClient.UserRegisteredOnServer(user));
                    return true;
                }

                _eventBus.Publish(this, new IUserApiClient.UserRegistrationFailed(user.Id, $"HTTP {response.StatusCode}"));
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[UserApiClient] Ошибка отправки пользователя: {ex.Message}");
                _eventBus.Publish(this, new IUserApiClient.UserRegistrationFailed(user.Id, ex.Message));
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            try
            {
                var response = await HttpClient.DeleteAsync($"/api/users/{userId}");

                if (response.IsSuccessStatusCode)
                {
                    // 🔥 Кричим, что сервер удалил
                    _eventBus.Publish(this, new IUserApiClient.UserDeletedOnServer(userId));
                    return true;
                }

                _eventBus.Publish(this, new IUserApiClient.UserDeleteFailed(userId, $"HTTP {response.StatusCode}"));
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[UserApiClient] Ошибка удаления пользователя: {ex.Message}");
                _eventBus.Publish(this, new IUserApiClient.UserDeleteFailed(userId, ex.Message));
                return false;
            }
        }

        public async Task<SyncBatchResponse<UserEntity>> FetchFreshUsersAsync(Guid deviceId, long lastVersion)
        {
            try
            {
                Logger.LogInfo($"[UserApiClient] Запрос обновлений пользователей с версии {lastVersion}...");
                string url = $"/api/sync/users?deviceId={deviceId}&lastVersion={lastVersion}";

                var response = await HttpClient.GetFromJsonAsync<SyncBatchResponse<UserEntity>>(url);
                return response ?? new SyncBatchResponse<UserEntity> { LatestVersion = lastVersion };
            }
            catch (Exception ex)
            {
                Logger.LogError($"[UserApiClient] Не удалось скачать обновления пользователей: {ex.Message}");
                return new SyncBatchResponse<UserEntity> { LatestVersion = lastVersion };
            }
        }

        public async Task<UserEntity?> GetUserByIdAsync(Guid id)
        {
            try
            {
                Logger.LogInfo($"[UserApiClient] Запрос юзера по ID: {id}...");
                return await HttpClient.GetFromJsonAsync<UserEntity>($"/api/users/{id}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[UserApiClient] Ошибка получения юзера {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<IReadOnlyCollection<UserEntity>> GetAllUsersAsync()
        {
            try
            {
                Logger.LogInfo("[UserApiClient] Запрос всех пользователей...");
                var response = await HttpClient.GetFromJsonAsync<List<UserEntity>>("/api/users");
                return response ?? new List<UserEntity>();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[UserApiClient] Ошибка получения списка: {ex.Message}");
                return Array.Empty<UserEntity>();
            }
        }


        public async Task<UserEntity?> AuthenticateAsync(string email, string password)
        {
            try
            {
                Logger.LogInfo($"[UserApiClient] Попытка входа для {email}...");

                var response = await HttpClient.PostAsJsonAsync("/api/users/auth", new { Email = email, Password = password });

                // 🔥 ЛОГИРУЕМ КОД ОТВЕТА ОТ СЕРВЕРА
                Logger.LogInfo($"[UserApiClient] Сервер ответил кодом: {(int)response.StatusCode} ({response.StatusCode})");

                if (response.IsSuccessStatusCode)
                {
                    var user = await response.Content.ReadFromJsonAsync<UserEntity>();
                    Logger.LogInfo($"[UserApiClient] Успешный вход. Юзер: {user?.Email ?? "null"}");
                    return user;
                }

                // 🔥 ЛОГИРУЕМ ТЕЛО ОШИБКИ (если сервер что-то прислал)
                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.LogWarning($"[UserApiClient] Сервер отклонил вход. Тело ответа: {errorContent}");

                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[UserApiClient] Ошибка при авторизации: {ex.Message}");
                return null;
            }
        }


        public async Task<bool> CheckEmailExistsAsync(string email)
        {
            try
            {
                var response = await HttpClient.GetFromJsonAsync<EmailCheckResponse>($"/api/users/check-email?email={Uri.EscapeDataString(email)}");
                return response?.Exists ?? false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[UserApiClient] Ошибка проверки Email: {ex.Message}");
                throw; // 🔥 ПРОБРАСЫВАЕМ ДАЛЬШЕ! Сервис сам поймет, что произошла ошибка.
            }
        }

    }



}
