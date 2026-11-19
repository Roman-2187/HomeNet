using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Dto;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.Api;
using System.Net.Http.Json;
using static HomeNet.Network.Api.IUserApiClient;

namespace HomeNet.Network.ApiClient
{
    public class UserApiClient(HttpClient httpClient, ICastomLogger logger, IEventBus eventBus) : BaseApiClient(httpClient, logger), IUserApiClient
    {
        private readonly IEventBus _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

        private class EmailCheckResponse
        {
            public bool Exists { get; set; }
        }
        public async Task<AuthResponse> PushUserAsync(UserEntity user)
        {
            try
            {
                var response = await HttpClient.PostAsJsonAsync("/api/users", user);

                if (response.IsSuccessStatusCode)
                {
                    var createdUser = await response.Content.ReadFromJsonAsync<UserEntity>() ?? user;
                    if (createdUser.SessionId != Guid.Empty)
                    {
                        SetSessionHeader(createdUser.SessionId);
                    }

                    _eventBus.Publish(this, new IUserApiClient.UserRegisteredOnServer(createdUser));
                    return new AuthResponse(true, createdUser, null);
                }

                string errorMessage = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Conflict => "Этот Email уже занят.",
                    System.Net.HttpStatusCode.BadRequest => "Некорректные данные при регистрации.",
                    _ => $"Ошибка сервера ({(int)response.StatusCode})."
                };

                Logger.LogWarning($"[UserApiClient] Ошибка регистрации ({(int)response.StatusCode}): {errorMessage}");
                _eventBus.Publish(this, new IUserApiClient.UserRegistrationFailed(user.Id, errorMessage));
                return new AuthResponse(false, null, errorMessage);
            }
            catch (HttpRequestException ex)
            {
                const string msg = "Сервер недоступен. Проверьте подключение к сети.";
                Logger.LogError($"[UserApiClient] Сервер недоступен при регистрации: {ex.Message}");
                _eventBus.Publish(this, new IUserApiClient.UserRegistrationFailed(user.Id, msg));
                return new AuthResponse(false, null, msg);
            }
            catch (TaskCanceledException ex)
            {
                const string msg = "Превышено время ожидания ответа от сервера.";
                Logger.LogError($"[UserApiClient] Таймаут при регистрации: {ex.Message}");
                _eventBus.Publish(this, new IUserApiClient.UserRegistrationFailed(user.Id, msg));
                return new AuthResponse(false, null, msg);
            }
            catch (Exception ex)
            {
                const string msg = "Внутренняя ошибка приложения при регистрации.";
                Logger.LogError($"[UserApiClient] Ошибка отправки пользователя: {ex.Message}");
                _eventBus.Publish(this, new IUserApiClient.UserRegistrationFailed(user.Id, ex.Message));
                return new AuthResponse(false, null, msg);
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



        public async Task<SyncBatchResponse<UserEntity>> FetchFreshUsersAsync(long sinceVersion)
        {
            try
            {
                Logger.LogInfo($"[UserApiClient] Запрос обновлений пользователей с версии {sinceVersion}...");
                var response = await HttpClient.GetFromJsonAsync<SyncBatchResponse<UserEntity>>($"/api/sync/users?sinceVersion={sinceVersion}");
                return response ?? new SyncBatchResponse<UserEntity>();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[UserApiClient] Не удалось скачать обновления пользователей: {ex.Message}");
                return new SyncBatchResponse<UserEntity>();
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





        public async Task<AuthResponse> AuthenticateAsync(string email, string password, Guid sessionId)
        {
            try
            {
                Logger.LogInfo($"[UserApiClient] Попытка входа для {email} (SessionId: {sessionId})...");

                var response = await HttpClient.PostAsJsonAsync(
                    "/api/users/auth",
                    new { Email = email, Password = password, SessionId = sessionId });

                if (response.IsSuccessStatusCode)
                {
                    var user = await response.Content.ReadFromJsonAsync<UserEntity>();
                    if (user != null && user.SessionId != Guid.Empty)
                    {
                        SetSessionHeader(user.SessionId);
                    }

                    Logger.LogInfo($"[UserApiClient] Успешный вход. Юзер: {user?.Email ?? "null"}, SessionId: {user?.SessionId}");
                    return new AuthResponse(true, user, null);
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.LogWarning($"[UserApiClient] Сервер отклонил вход ({(int)response.StatusCode}): {errorContent}");

                string errorMessage = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? "Неверный Email или пароль."
                    : $"Ошибка сервера ({(int)response.StatusCode}).";

                return new AuthResponse(false, null, errorMessage);
            }
            catch (HttpRequestException ex)
            {
                Logger.LogError($"[UserApiClient] Сервер недоступен при авторизации: {ex.Message}");
                return new AuthResponse(false, null, "Сервер недоступен. Проверьте подключение к сети.");
            }
            catch (TaskCanceledException ex)
            {
                Logger.LogError($"[UserApiClient] Таймаут при авторизации: {ex.Message}");
                return new AuthResponse(false, null, "Превышено время ожидания ответа от сервера.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[UserApiClient] Непредвиденная ошибка при авторизации: {ex.Message}");
                return new AuthResponse(false, null, "Внутренняя ошибка приложения при входе.");
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
