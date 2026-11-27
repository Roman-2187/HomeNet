using HomeNet.Network.Api;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetCore.Interfaces.Services;
using static HomeNetCore.Interfaces.Services.IProfileService;

namespace HomeNetServices.Identity
{
    public class ProfileService(IUserApiClient apiClient, IUserRepo userRepo) : IProfileService
    {
        private readonly IUserApiClient _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        private readonly IUserRepo _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));

        public async Task<ProfileVerdict> DeleteAccountAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                return new ProfileVerdict(false, "Некорректный идентификатор пользователя.");

            try
            {
                // 1. Сначала просим сервер физически снести профиль и записать надгробие (Id, Version, DeletedAt)
                var apiSuccess = await _apiClient.DeleteUserAsync(userId);
                if (!apiSuccess)
                    return new ProfileVerdict(false, "Сервер отклонил запрос на удаление профиля.");

                // 2. Если сервер подтвердил — удаляем запись из локальной SQLite
                await _userRepo.DeleteByIdAsync(userId);

                return new ProfileVerdict(true, string.Empty);
            }
            catch (Exception ex)
            {
                return new ProfileVerdict(false, $"Ошибка при удалении профиля: {ex.Message}");
            }
        }
    }
}