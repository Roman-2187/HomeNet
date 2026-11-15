using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Network.Api;
using HomeNet.Synchronization.Interfaces.Sinc;
using HomeNetCore.Interfaces.ViewModels.UserZone;
using HomeNetOrm.Exeptions;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Local;
using static HomeNetCore.Interfaces.ViewModels.UserZone.IDeviceSettingsVM;

namespace HomeNet.Synchronization.Sinc
{
    public class UserSyncService : ISyncCoordinator, IDisposable
    {
        private readonly IUserRepo _userRepository;
        private readonly ClientDeviceSettingsRepository _deviceSettingsRepo;
        private readonly IUserApiClient _apiClient;
        private readonly IEventBus _eventBus;
        private readonly ICastomLogger _logger;
        private bool _isInitialSyncExecuted;

        public UserSyncService(
            IUserRepo userRepository,
            ClientDeviceSettingsRepository deviceSettingsRepo,
            IUserApiClient apiClient,
            IEventBus eventBus,
            ICastomLogger logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _deviceSettingsRepo = deviceSettingsRepo ?? throw new ArgumentNullException(nameof(deviceSettingsRepo));
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            SubscribeEvents();
        }

        public async Task StartAsync()
        {
            if (_isInitialSyncExecuted) return;
            _isInitialSyncExecuted = true;

            _logger.LogInfo("=== [UserSyncService] ЗАПУСК СТАРТОВОЙ СИНХРОНИЗАЦИИ ===");
            await PullAsync();
            await PushAsync();
            _logger.LogInfo("=== [UserSyncService] СТАРТОВАЯ СИНХРОНИЗАЦИЯ ЗАВЕРШЕНА ===");
        }

        private async Task PullAsync()
        {
            try
            {
                // 1. Читаем активную сессию напрямую из репозитория (строго по иерархии слоёв!)
                var activeSession = await _deviceSettingsRepo.GetActiveSessionAsync();
                Guid deviceId = activeSession?.DeviceId ?? Guid.Empty;
                long lastVersion = activeSession?.LastSyncVersion ?? 0;

                _logger.LogInfo($"[UserSyncService] Запрос PULL с версии {lastVersion} (DeviceId: {deviceId})...");

                // 2. Забираем пакет с сервера: живые изменения + надгробия + новая версия
                var batch = await _apiClient.FetchFreshUsersAsync(deviceId, lastVersion);

                // 3. Вставляем или обновляем живых пользователей
                foreach (var user in batch.UpdatedItems)
                {
                    user.IsSynced = 1;

                    try
                    {
                        await _userRepository.InsertAsync(user);
                    }
                    catch (DuplicateEmailException)
                    {
                        var existing = await _userRepository.GetByEmailAsync(user.Email!);
                        if (existing != null)
                        {
                            existing.IsSynced = 1;
                            existing.DeviceId = user.DeviceId;
                            existing.FirstName = user.FirstName;
                            existing.LastName = user.LastName;
                            existing.PhoneNumber = user.PhoneNumber;

                            await _userRepository.UpdateAsync(existing);
                            _logger.LogDebug($"[UserSyncService] Юзер {user.Email} обновлен (IsSynced=1).");
                        }
                    }
                }

                // 4. Вычищаем из SQLite всех пользователей по надгробиям с сервера (и из users, и из device_settings)
                foreach (var deletedId in batch.DeletedIds)
                {
                    await _userRepository.DeleteByIdAsync(deletedId);
                    await _deviceSettingsRepo.DeleteByUserIdAsync(deletedId);

                    _logger.LogInfo($"[UserSyncService] Надгробие применено: юзер {deletedId} вычищен из users и device_settings.");

                    if (activeSession != null && activeSession.UserId == deletedId)
                    {
                        activeSession = null;
                        _logger.LogWarning($"[UserSyncService] Текущий активный аккаунт ({deletedId}) был удалён на сервере!");
                    }
                }

                // 5. Обновляем закладку версии напрямую через репозиторий
                if (activeSession != null && batch.LatestVersion > lastVersion)
                {
                    activeSession.LastSyncVersion = batch.LatestVersion;
                    await _deviceSettingsRepo.SaveSettingsAsync(activeSession);
                    _logger.LogInfo($"[UserSyncService] Закладка версии обновлена: {lastVersion} -> {batch.LatestVersion}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[UserSyncService] Ошибка PULL: {ex.Message}");
            }
        }




        private async Task PushAsync()
        {
            try
            {
                var allUsers = await _userRepository.GetAllAsync();
                var pending = allUsers.Where(u => u.IsSynced == 0).ToList();

                if (pending.Count > 0)
                {
                    _logger.LogWarning($"[UserSyncService] Обнаружено {pending.Count} неотправленных юзеров. Инициируем PUSH.");
                    foreach (var user in pending)
                    {
                        await _apiClient.PushUserAsync(user);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[UserSyncService] Ошибка PUSH: {ex.Message}");
            }
        }

        private void SubscribeEvents()
        {
            // 🔥 Подписываемся на async Task, а не async void
            _eventBus.SubscribeAsync<IUserApiClient.UserRegisteredOnServer>(OnUserRegistered);
            _eventBus.SubscribeAsync<IUserApiClient.UserDeletedOnServer>(OnUserDeleted);
            _eventBus.SubscribeAsync<UserReceivedFromNetwork>(OnUserReceivedFromNetwork);
            _eventBus.SubscribeAsync<UserDeletedFromNetwork>(OnUserDeletedFromNetwork);
            _eventBus.Subscribe<SessionPersisted>(OnSessionPersisted);
        }

        private async void OnSessionPersisted( SessionPersisted e)
        {
            _logger.LogInfo($"[UserSyncService] Сессия готова в SQLite (DeviceId: {e.DeviceId}, UserId: {e.UserId}). Начинаем синхронизацию...");
            await PullAsync();
        }

        // 🔥 Возвращаем Task, а не void
        private async Task OnUserRegistered(IUserApiClient.UserRegisteredOnServer msg)
        {
            try
            {
                _logger.LogInfo($"[UserSyncService] Сервер подтвердил юзера {msg.User.Id}. Пишем в SQLite.");
                await _userRepository.InsertAsync(msg.User);
            }
            catch (DuplicateEmailException)
            {
                _logger.LogDebug($"[UserSyncService] Юзер {msg.User.Email} уже есть в кэше. Пропускаем.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[UserSyncService] Ошибка при сохранении юзера: {ex.Message}");
            }
        }

        private async Task OnUserDeleted(IUserApiClient.UserDeletedOnServer msg)
        {
            try
            {
                _logger.LogInfo($"[UserSyncService] Сервер удалил юзера {msg.UserId}. Стираем локально.");
                await _userRepository.DeleteByIdAsync(msg.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[UserSyncService] Ошибка удаления: {ex.Message}");
            }
        }

        private async Task OnUserReceivedFromNetwork(UserReceivedFromNetwork msg)
        {
            try
            {
                _logger.LogInfo($"[UserSyncService] Сеть прислала юзера {msg.User.Id}. Пишем в SQLite.");
                await _userRepository.InsertAsync(msg.User);
            }
            catch (DuplicateEmailException)
            {
                _logger.LogDebug($"[UserSyncService] Юзер {msg.User.Email} уже есть в кэше. Пропускаем.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[UserSyncService] Ошибка сохранения: {ex.Message}");
            }
        }

        private async Task OnUserDeletedFromNetwork(UserDeletedFromNetwork msg)
        {
            try
            {
                _logger.LogInfo($"[UserSyncService] Сеть прислала удаление юзера {msg.UserId}. Стираем локально.");
                await _userRepository.DeleteByIdAsync(msg.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[UserSyncService] Ошибка удаления: {ex.Message}");
            }
        }

        public void Dispose()
        {
            // 🔥 Отписываемся так же (EventBus сам разберется)
            _eventBus.UnsubscribeAsync<IUserApiClient.UserRegisteredOnServer>(OnUserRegistered);
            _eventBus.UnsubscribeAsync<IUserApiClient.UserDeletedOnServer>(OnUserDeleted);
            _eventBus.UnsubscribeAsync<UserReceivedFromNetwork>(OnUserReceivedFromNetwork);
            _eventBus.UnsubscribeAsync<UserDeletedFromNetwork>(OnUserDeletedFromNetwork);
        }
    }
} 