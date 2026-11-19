using HomeNet.Models.Shared.Dto;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.ApiClient;

namespace HomeNet.Network.Api
{
    public interface IUserApiClient : IApiClientBase
    {
       
        Task<AuthResponse> PushUserAsync(UserEntity user);
        Task<bool> DeleteUserAsync(Guid userId);
      



        // 🔥 Новые методы для работы с сервером (чтение и поиск)
        Task<UserEntity?> GetUserByIdAsync(Guid id);
        Task<IReadOnlyCollection<UserEntity>> GetAllUsersAsync();

        Task<bool> CheckEmailExistsAsync(string email);

        Task<AuthResponse> AuthenticateAsync(string email, string password, Guid sessionId);

        Task<SyncBatchResponse<UserEntity>> FetchFreshUsersAsync(long sinceVersion);


        // 🔥 ФАКТЫ, которые публикует сам API-клиент
        public record UserRegisteredOnServer(UserEntity User);
        public record UserRegistrationFailed(Guid UserId, string Error);

        public record AuthResponse(bool IsSuccess, UserEntity? User, string? ErrorMessage);

        public record UserDeletedOnServer(Guid UserId);
        public record UserDeleteFailed(Guid UserId, string Error);
    }
}