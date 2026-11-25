using CommunityToolkit.Mvvm.ComponentModel;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Interfaces.ViewModels.AdminZone;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetPresentation.ViewModels.MainViews;
using static HomeNetCore.Interfaces.ViewModels.MainZone.IStatusBarViewModel;
using static HomeNetCore.Interfaces.ViewModels.UserZone.IDeviceSettingsVM;
using static HomeNetCore.Interfaces.ViewModels.UserZone.IUserVm;
using static HomeNetOrm.Interfaces.Builders.IDbContextContainer;

namespace HomeNetPresentation.ViewModels.ClientViews
{
    /// <summary>
    /// Вьюмодель экрана настроек устройства и управления активной сессией.
    /// Перехватывает события авторизации, управляет автовходом и очисткой сессии.
    /// </summary>
    public sealed partial class DeviceSettingsViewModel : FormViewModelBase
    {
        private readonly IDeviceSettingsService _deviceSettingsService;
        private readonly IUserRepo _userRepository;

        [ObservableProperty]
        public partial bool IsRememberMeChecked { get; set; } = true;

        [ObservableProperty]
        public partial string DeviceIdDisplay { get; set; } = "Загрузка...";

        [ObservableProperty]
        public partial string CurrentSessionEmail { get; set; } = "Гостевой режим";

        public DeviceSettingsViewModel(
            IEventBus eventBus,
            ICastomLogger logger,
            IDeviceSettingsService deviceSettingsService,
            IUserRepo userRepository) : base(eventBus, logger)
        {
            _deviceSettingsService = deviceSettingsService ?? throw new ArgumentNullException(nameof(deviceSettingsService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

            EventBus.SubscribeAsync<UserSignedIn>(OnUserSessionActivated);
            EventBus.SubscribeAsync<UserSignedUp>(OnUserSessionActivated);
            EventBus.SubscribeAsync<LogoutClient>(OnClientLogoutRequested);
            EventBus.SubscribeAsync<IAdminVm.LogoutAdmin>(OnAdminLogoutRequested);
            EventBus.Subscribe<InfrastructureReady>(OnInfrastructureReady);
        }

        private void OnInfrastructureReady(InfrastructureReady msg)
        {
            Logger.LogInfo("[AutoLogin] Получен сигнал, что база готова. Запускаем проверку сессии.");

            _ = LoadHardwareIdAsync();
            _ = TryAutoLoginAsync();
        }

        private async Task LoadHardwareIdAsync()
        {
            try
            {
                var deviceId = await _deviceSettingsService.GetDeviceIdAsync();
                DeviceIdDisplay = deviceId.ToString().ToUpperInvariant();
            }
            catch (Exception ex)
            {
                DeviceIdDisplay = "Ошибка ID";
                Logger.LogError($"[DeviceSettings] Не удалось получить ID устройства: {ex.Message}");
            }
        }

        private async Task TryAutoLoginAsync()
        {
            try
            {
                Logger.LogInfo("[AutoLogin] Проверяем наличие сохраненной сессии на устройстве...");

                var settings = await _deviceSettingsService.GetCurrentSettingsAsync();

                if (settings == null ||
                    settings.Id == Guid.Empty ||
                    settings.UserId == Guid.Empty ||
                    string.IsNullOrEmpty(settings.Email))
                {
                    Logger.LogInfo("[AutoLogin] Чистый запуск. Сохраненной сессии пользователя нет.");
                    return;
                }

                Logger.LogInfo($"[AutoLogin] Найдена сессия {settings.Id} для {settings.Email}. Загружаем профиль из SQLite...");

                var localUser = await _userRepository.GetByIdAsync(settings.UserId);

                if (localUser != null)
                {
                    localUser.SessionId = settings.Id;
                    Logger.LogInfo("[AutoLogin] Сессия валидна. Генерируем событие входа для всей экосистемы SiberNet.");

                    EventBus.Publish(this, new UserSignedIn(localUser));
                    EventBus.Publish(this, new TextChanged("[AutoLogin] выполнен вход"));
                }
                else
                {
                    Logger.LogWarning("[AutoLogin] Профиль пользователя не найден в локальной базе.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[AutoLogin] Сбой авто-авторизации: {ex.Message}");
            }
        }

        private async Task OnUserSessionActivated(object msg)
        {
            var user = msg switch
            {
                UserSignedIn signIn => signIn.User,
                UserSignedUp signUp => signUp.User,
                _ => null
            };

            if (user == null) return;

            try
            {
                if (CurrentSessionEmail == user.Email) return;

                CurrentSessionEmail = user.Email ?? "Авторизован";
                var appDeviceId = await _deviceSettingsService.GetDeviceIdAsync();

                if (user.SessionId != Guid.Empty)
                {
                    user.IsSynced = 1;
                }

                await _userRepository.UpdateAsync(user);

                if (IsRememberMeChecked)
                {
                    await _deviceSettingsService.SaveSessionAsync(user.SessionId, user.Id, user.Email!);
                }

                Logger.LogInfo($"[Сессия -> VM] Локальный SQLite обновлен (SessionId: {user.SessionId}). Даем отмашку синхронизатору.");

                EventBus.Publish(this, new SessionPersisted(appDeviceId, user.Id));
            }
            catch (Exception ex)
            {
                Logger.LogError($"[Сессия -> VM] Ошибка при обработке активации сессии: {ex.Message}");
            }
        }

        private Task OnClientLogoutRequested(LogoutClient msg) => ClearSessionAsync("LogoutClient");

        private Task OnAdminLogoutRequested(IAdminVm.LogoutAdmin msg) => ClearSessionAsync("LogoutAdmin");

        private async Task ClearSessionAsync(string source)
        {
            try
            {
                Logger.LogInfo($"[Сессия -> VM] Поймали {source}. Выжигаем сохраненную сессию из настроек.");

                await _deviceSettingsService.ClearSessionAsync();
                CurrentSessionEmail = "Гостевой режим";

                EventBus.Publish(this, new TextChanged("Logout"));
            }
            catch (Exception ex)
            {
                Logger.LogError($"[Сессия -> VM] Ошибка при очистке сессии при разлогине ({source}): {ex.Message}");
            }
        }

        public override void Dispose()
        {
            EventBus.UnsubscribeAsync<UserSignedIn>(OnUserSessionActivated);
            EventBus.UnsubscribeAsync<UserSignedUp>(OnUserSessionActivated);
            EventBus.UnsubscribeAsync<LogoutClient>(OnClientLogoutRequested);
            EventBus.UnsubscribeAsync<IAdminVm.LogoutAdmin>(OnAdminLogoutRequested);
            EventBus.Unsubscribe<InfrastructureReady>(OnInfrastructureReady);

            base.Dispose();
        }
    }
}
