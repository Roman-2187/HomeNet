using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.Api;
using HomeNet.Synchronization.Interfaces.Sinc;
using HomeNetOrm.Exeptions;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Local;

namespace HomeNet.Synchronization.Items
{
    public class UserSyncItem : IEntitySyncItem
    {
        private readonly IUserRepo _userRepository;
        private readonly ClientSettingsRepo _deviceSettingsRepo;
        private readonly IUserApiClient _apiClient;
        private readonly ICastomLogger _logger;

        public int Order => 1;

        public UserSyncItem(
            IUserRepo userRepository,
            ClientSettingsRepo deviceSettingsRepo,
            IUserApiClient apiClient,
            ICastomLogger logger)
        {
            _userRepository = userRepository;
            _deviceSettingsRepo = deviceSettingsRepo;
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task PullAsync(CancellationToken ct = default)
        {
            try
            {
                var session = await _deviceSettingsRepo.GetActiveSessionAsync();
                if (session == null || session.Id == Guid.Empty) return;

                _logger.LogInfo($"[UserSyncItem] Запрос обновлений профилей (SessionId: {session.Id})...");
                var batch = await _apiClient.FetchFreshUsersAsync();

                await UpsertUsersAsync(batch.UpdatedItems);
                await ApplyTombstonesAsync(batch.DeletedIds, session);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[UserSyncItem] Ошибка PULL: {ex.Message}");
            }
        }


        public async Task PushAsync(CancellationToken ct = default)
        {
            try
            {
                var allUsers = await _userRepository.GetAllAsync();
                var pending = allUsers.Where(u => u.IsSynced == 0).ToList();

                foreach (var user in pending)
                {
                    await _apiClient.PushUserAsync(user);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[UserSyncItem] Ошибка PUSH: {ex.Message}");
            }
        }

        private async Task UpsertUsersAsync(IEnumerable<UserEntity> users)
        {
            foreach (var user in users)
            {
                user.IsSynced = 1;
                try
                {
                    await _userRepository.InsertAsync(user);
                }
                catch (DuplicateEmailException)
                {
                    var existing = await _userRepository.GetByEmailAsync(user.Email!);
                    if (existing == null) continue;

                    existing.IsSynced = 1;
                    existing.DeviceId = user.DeviceId;
                    existing.FirstName = user.FirstName;
                    existing.LastName = user.LastName;
                    existing.PhoneNumber = user.PhoneNumber;
                    await _userRepository.UpdateAsync(existing);
                }
            }
        }

        private async Task<SettingsEntity?> ApplyTombstonesAsync(IEnumerable<Guid> deletedIds, SettingsEntity? session)
        {
            foreach (var id in deletedIds)
            {
                await _userRepository.DeleteByIdAsync(id);
                await _deviceSettingsRepo.DeleteByUserIdAsync(id);

                if (session != null && session.UserId == id)
                {
                    session = null;
                }
            }
            return session;
        }
    }
}
