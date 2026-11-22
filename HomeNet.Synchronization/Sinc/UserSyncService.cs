using HomeNet.Infrastructure.Shared;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.Api;
using HomeNet.Network.SignalRListeners;
using HomeNetOrm.Exceptions;
using HomeNetOrm.Interfaces.Repositories;

namespace HomeNet.Synchronization.Sinc
{
    public class UserSyncService : IDisposable
    {
        private readonly IUserRepo _userRepository;
        private readonly IEventBus _eventBus;
        private readonly ICastomLogger _logger;

        public UserSyncService(
            IUserRepo userRepository,
            IEventBus eventBus,
            ICastomLogger logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            _eventBus.SubscribeAsync<IUserApiClient.UserRegisteredOnServer>(OnUserRegistered);
            _eventBus.SubscribeAsync<IUserApiClient.UserDeletedOnServer>(OnUserDeleted);
            _eventBus.SubscribeAsync<UserReceivedFromNetwork>(OnUserReceivedFromNetwork);
            _eventBus.SubscribeAsync<UserDeletedFromNetwork>(OnUserDeletedFromNetwork);
        }

        private async Task OnUserRegistered(IUserApiClient.UserRegisteredOnServer msg)
        {
            _logger.LogInfo($"[UserRealtime] Сервер подтвердил юзера {msg.User.Id}. Пишем в SQLite.");
            await SaveUserSafeAsync(msg.User);
        }

        private async Task OnUserReceivedFromNetwork(UserReceivedFromNetwork msg)
        {
            _logger.LogInfo($"[UserRealtime] Сеть прислала юзера {msg.User.Id}. Пишем в SQLite.");
            await SaveUserSafeAsync(msg.User);
        }

        private async Task OnUserDeleted(IUserApiClient.UserDeletedOnServer msg)
        {
            _logger.LogInfo($"[UserRealtime] Сервер удалил юзера {msg.UserId}. Стираем локально.");
            await DeleteUserSafeAsync(msg.UserId);
        }

        private async Task OnUserDeletedFromNetwork(UserDeletedFromNetwork msg)
        {
            _logger.LogInfo($"[UserRealtime] Сеть прислала удаление юзера {msg.UserId}. Стираем локально.");
            await DeleteUserSafeAsync(msg.UserId);
        }

        private async Task SaveUserSafeAsync(UserEntity user)
        {
            try
            {
                user.IsSynced = 1;
                await _userRepository.InsertAsync(user);
            }
            catch (DuplicateEmailException)
            {
                _logger.LogDebug($"[UserRealtime] Юзер {user.Email} уже есть в кэше. Пропускаем.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[UserRealtime] Ошибка сохранения юзера {user.Id}: {ex.Message}");
            }
        }

        private async Task DeleteUserSafeAsync(Guid userId)
        {
            try
            {
                await _userRepository.DeleteByIdAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[UserRealtime] Ошибка удаления юзера {userId}: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _eventBus.UnsubscribeAsync<IUserApiClient.UserRegisteredOnServer>(OnUserRegistered);
            _eventBus.UnsubscribeAsync<IUserApiClient.UserDeletedOnServer>(OnUserDeleted);
            _eventBus.UnsubscribeAsync<UserReceivedFromNetwork>(OnUserReceivedFromNetwork);
            _eventBus.UnsubscribeAsync<UserDeletedFromNetwork>(OnUserDeletedFromNetwork);
        }
    }
}
