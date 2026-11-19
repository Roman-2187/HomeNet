using HomeNet.Models.Shared.Dto;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.ApiClient;

namespace HomeNet.Network.Api
{
    /// <summary>
    /// Сетевой клиент для взаимодействия с API пользователей, авторизации и дельта-синхронизации профилей.
    /// </summary>
    public interface IUserApiClient : IApiClient
    {
        /// <summary>
        /// Регистрирует нового пользователя на сервере и возвращает результат с созданной сессией.
        /// </summary>
        Task<AuthResponse> RegisterUserAsync(UserEntity user);

        /// <summary>
        /// Выполняет вход по Email и паролю, привязывая указанный SessionId на сервере.
        /// </summary>
        Task<AuthResponse> AuthenticateAsync(string email, string password, Guid sessionId);

        /// <summary>
        /// Удаляет профиль пользователя на сервере (создаёт надгробие для остальных клиентов).
        /// </summary>
        Task<bool> DeleteUserAsync(Guid userId);

        /// <summary>
        /// Проверяет, занят ли указанный Email в базе данных сервера.
        /// </summary>
        Task<bool> CheckEmailExistsAsync(string email);

        /// <summary>
        /// Получает актуальные данные профиля пользователя по его уникальному идентификатору.
        /// </summary>
        Task<UserEntity?> GetUserByIdAsync(Guid id);

        /// <summary>
        /// Загружает полный список профилей с сервера (используется для начальной инициализации или поиска).
        /// </summary>
        Task<IReadOnlyCollection<UserEntity>> GetAllUsersAsync();

        /// <summary>
        /// Загружает дельту изменённых профилей и надгробий (удалённых аккаунтов), начиная с указанной версии.
        /// </summary>
        Task<SyncBatchResponse<UserEntity>> FetchFreshUsersAsync(long sinceVersion);

        // 🔥 Контракты ответов и событий, которые публикует API-клиент

        /// <summary>
        /// Ответ сервера при проверке занятости Email.
        /// </summary>
        public record EmailCheckResponse(bool Exists);

        /// <summary>
        /// Результат попытки регистрации или входа на сервере.
        /// </summary>
        public record AuthResponse(bool IsSuccess, UserEntity? User, string? ErrorMessage);

        /// <summary>
        /// Событие успешной регистрации пользователя на сервере.
        /// </summary>
        public record UserRegisteredOnServer(UserEntity User);

        /// <summary>
        /// Событие сбоя при регистрации пользователя на сервере.
        /// </summary>
        public record UserRegistrationFailed(Guid UserId, string Error);

        /// <summary>
        /// Событие успешного удаления профиля на сервере.
        /// </summary>
        public record UserDeletedOnServer(Guid UserId);

        /// <summary>
        /// Событие ошибки при попытке удалить профиль на сервере.
        /// </summary>
        public record UserDeleteFailed(Guid UserId, string Error);
    }
}
