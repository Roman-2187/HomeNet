using CommunityToolkit.Mvvm.ComponentModel;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Interfaces.ViewModels.AdminZone;
using HomeNetCore.Interfaces.ViewModels.MainZone;
using HomeNetCore.Interfaces.ViewModels.UserZone;
using HomeNetOrm.Interfaces.Repositories;
using static HomeNetOrm.Interfaces.Builders.IDbContextContainer;
using UserNavigationManager = HomeNetPresentation.Serviсes.UserNavigationManager;

namespace HomeNetPresentation.ViewModels.ClientViews
{
    /// <summary>
    /// Вьюмодель экрана настроек устройства и управления активной сессией.
    /// Перехватывает события авторизации, управляет автовходом и очисткой сессии! 🧼
    /// </summary>
    public partial class DeviceSettingsViewModel : FormViewModelBase<UserNavigationManager>, IDisposable
    {
        private readonly IDeviceSettingsService _deviceSettingsService;
        private readonly ICastomLogger _logger;
        private readonly IUserRepo _userRepository;

        [ObservableProperty] private bool _isRememberMeChecked = true;
        [ObservableProperty] private string _deviceIdDisplay = "Загрузка...";
        [ObservableProperty] private string _currentSessionEmail = "Гостевой режим";

        public DeviceSettingsViewModel(
            IDeviceSettingsService deviceSettingsService,
            IUserRepo userRepository,
            IEventBus eventBus,
            ICastomLogger logger,
            UserNavigationManager navigation) : base(eventBus, navigation)
        {
            _deviceSettingsService = deviceSettingsService;
          _userRepository = userRepository;
            _logger = logger;

            // Подписываемся на успех входа/регистрации
            _eventBus.Subscribe<IUserVm.UserSignedIn>(OnUserSessionActivated);
            _eventBus.Subscribe<IUserVm.UserSignedUp>(OnUserSessionActivated);

            // 🔥 Обязательно слушаем разлогин, чтобы обнулить данные сессии
            _eventBus.Subscribe<IUserVm.LogoutClient>(OnLogoutRequested);
            // Оставляешь старые подписки как есть
            _eventBus.Subscribe<IUserVm.LogoutClient>(OnLogoutRequested);

            // 🔥 Для админа подписываешься через лямбду, передавая null вместо клиентского сообщения
            _eventBus.Subscribe<IAdminVm.LogoutAdmin>(_ => OnLogoutRequested(null!));


            _eventBus.Subscribe<InfrastructureReady>(OnInfrastructureReady);

           
        }


        private void OnInfrastructureReady(InfrastructureReady msg)
        {
            _logger.LogInfo("[AutoLogin] Получен сигнал, что база готова. Запускаем проверку сессии.");

         // Инициализация железа
            Task.Run(LoadHardwareIdAsync);

            // 🔥 При старте ВьюМодели проверяем: если есть сохраненные данные — сразу ломимся в сервис
            Task.Run(TryAutoLoginAsync);
           
        }

        private async Task LoadHardwareIdAsync()
        {
            Guid deviceId = await _deviceSettingsService.GetDeviceIdAsync();
            DeviceIdDisplay = deviceId.ToString().ToUpper();
        }

        private async Task TryAutoLoginAsync()
        {
            try
            {
                _logger.LogInfo("[AutoLogin] Проверяем наличие сохраненной сессии на устройстве...");

                SettingsEntity? settings = await _deviceSettingsService.GetCurrentSettingsAsync();

                // Проверяем наличие активной сессии без пароля!
                if (settings == null ||
                    settings.Id == Guid.Empty ||
                    settings.UserId == Guid.Empty ||
                    string.IsNullOrEmpty(settings.Email))
                {
                    _logger.LogInfo("[AutoLogin] Чистый запуск. Сохраненной сессии пользователя нет.");
                    return;
                }

                _logger.LogInfo($"[AutoLogin] Найдена сессия {settings.Id} для {settings.Email}. Загружаем профиль из SQLite...");

                UserEntity? localUser = await _userRepository.GetByIdAsync(settings.UserId);

                if (localUser != null)
                {
                    localUser.SessionId = settings.Id;
                    _logger.LogInfo("[AutoLogin] Сессия валидна. Генерируем событие входа для всей экосистемы SiberNet.");

                    _eventBus.Publish(this, new IUserVm.UserSignedIn(localUser));
                    _eventBus.Publish(this, new IStatusBarViewModel.TextChanged("[AutoLogin] выполнен вход"));
                }
                else
                {
                    _logger.LogWarning("[AutoLogin] Профиль пользователя не найден в локальной базе.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AutoLogin] Сбой авто-авторизации: {ex.Message}");
            }
        }

        private async void OnUserSessionActivated(object msg)
        {
            UserEntity? user = msg switch
            {
                IUserVm.UserSignedIn signIn => signIn.User,
                IUserVm.UserSignedUp signUp => signUp.User,
                _ => null
            };

            if (user == null) return;

            try
            {
                if (CurrentSessionEmail == user.Email) return;

                CurrentSessionEmail = user.Email ?? "Авторизован";
                Guid appDeviceId = await _deviceSettingsService.GetDeviceIdAsync();

                // Если регистрация прошла с сервером — помечаем как синхронизированного, чтобы SyncBatchFactory не слал дубль!
                if (user.SessionId != Guid.Empty)
                {
                    user.IsSynced = 1;
                }

                await _userRepository.UpdateAsync(user);

                if (IsRememberMeChecked)
                {
                    await _deviceSettingsService.SaveSessionAsync(user.SessionId, user.Id, user.Email!);
                }

                _logger.LogInfo($"[Сессия -> VM] Локальный SQLite обновлен (SessionId: {user.SessionId}). Даем отмашку синхронизатору.");

                _eventBus.Publish(this, new IDeviceSettingsVM.SessionPersisted(appDeviceId, user.Id));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Сессия -> VM] Ошибка при обработке активации сессии: {ex.Message}");
            }
        }



        /// <summary>
        /// 🔥 Перехватчик ЛОГАУТА: Обнуляет записанные данные в текущей сессии
        /// </summary>
        private async void OnLogoutRequested(IUserVm.LogoutClient msg)
        {
            try
            {
                _logger.LogInfo("[Сессия -> VM] Поймали LogoutClient. Выжигаем сохраненную сессию из настроек.");

                // Стираем данные из таблицы настроек устройства
                await _deviceSettingsService.ClearSessionAsync();

                CurrentSessionEmail = "Гостевой режим";
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Сессия -> VM] Ошибка при очистке сессии при разлогине: {ex.Message}");
            }
        }


        public override void Dispose()
        {
            base.Dispose();
            _eventBus.Unsubscribe<IUserVm.UserSignedIn>(OnUserSessionActivated);
            _eventBus.Unsubscribe<IUserVm.UserSignedUp>(OnUserSessionActivated);
            _eventBus.Unsubscribe<IUserVm.LogoutClient>(OnLogoutRequested);
        }
    }
}
