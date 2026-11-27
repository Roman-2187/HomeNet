using HomeNet.DI.Modules;
using HomeNet.Infrastructure.Shared.Diagnostics.EventInspector;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Network.SignalRListeners;
using HomeNetCore.Enums;
using HomeNetOrm.Schemes;
using HomeNetPresentation.ViewModels.AdminViews;
using HomeNetPresentation.ViewModels.ClientViews;
using HomeNetPresentation.ViewModels.ClientViews.Chat;
using HomeNetPresentation.ViewModels.MainViews;

namespace HomeNet.DI
{
    public class AppCompositionRoot
    {
        private static AppCompositionRoot? _instance;
        public static AppCompositionRoot Instance => _instance
            ?? throw new InvalidOperationException("Композиция не собрана. Вызовите AppCompositionRoot.Initialize(...)");

        // Модули делаем internal, чтобы Bootstrapper мог достучаться до контейнеров бэкенда
        internal readonly CoreInfrastructureModule Infrastructure;
        public readonly DataAccessModule DataAccess;
        internal readonly BusinessServicesModule Services;
        internal readonly PresentationUiModule Ui;

        // 🔥 Прокидываем свойства для быстрого доступа
        public ICastomLogger Logger => Infrastructure.Logger;
        public IEventBus EventBus => Infrastructure.EventBus;
        
        public EventBusInspector EventBusInspector => Infrastructure.EventBusInspector;

        // Единый клиент SignalR вместо двух раздельных слушателей
        public SignalRHubClient HubClient => DataAccess.HubClient;

        private AppCompositionRoot(BackendMode mode, string apiBaseUrl, string sqliteConn)
        {
            Infrastructure = new CoreInfrastructureModule();

            var schemaModule = new LocalMessengerSchemaModule();
            var registeredSchemas = schemaModule.GetSchemas();

            DataAccess = new DataAccessModule(
                sqliteConn,
                apiBaseUrl,
                registeredSchemas,
                Infrastructure.Logger,
                Infrastructure.LogQueueManager,
                Infrastructure.EventBus);

            Services = new BusinessServicesModule(
                  DataAccess.UserRepository,          // Для чтения из кэша
                  DataAccess.MessageRepository,       // Для чтения из кэша
                  DataAccess.DeviceSettingsRepository,// Локальные настройки
                  DataAccess.UserApiClient,           // 🔥 ДЛЯ РЕГИСТРАЦИИ И АУТЕНТИФИКАЦИИ
                  DataAccess.MessageApiClient,        // 🔥 ДЛЯ ОТПРАВКИ СООБЩЕНИЙ
                  Infrastructure.Logger,
                  Infrastructure.EventBus);

            Ui = new PresentationUiModule(
                Infrastructure.EventBus,
                Infrastructure.Logger,
                Services,
                Infrastructure.EventBusInspector
            );
        }

        /// <summary>
        /// Чистая фабричная сборка без скрытых фоновых тасок и подписок
        /// </summary>
        public static void Initialize(BackendMode mode, string apiBaseUrl, string sqliteConn)
        {
            _instance = new AppCompositionRoot(mode, apiBaseUrl, sqliteConn);
        }

        public TGet GetViewModel<TGet>() where TGet : class
        {
            if (typeof(TGet) == typeof(TableUsersViewModel)) return (Ui.TableUsersVm as TGet)!;
            if (typeof(TGet) == typeof(UserViewModel)) return (Ui.UserVm as TGet)!;
            if (typeof(TGet) == typeof(MainViewModel)) return (Ui.MainVm as TGet)!;
            if (typeof(TGet) == typeof(ChatViewModel)) return (Ui.ChatVm as TGet)!;
            if (typeof(TGet) == typeof(StatusBarViewModel)) return (Ui.StatusBarVm as TGet)!;
            if (typeof(TGet) == typeof(SystemButtonsViewModel)) return (Ui.SystemButtonsVm as TGet)!;
            if (typeof(TGet) == typeof(RegistrationViewModel)) return (Ui.RegistrationVm as TGet)!;
            if (typeof(TGet) == typeof(AuthenticationViewModel)) return (Ui.AuthenticationVm as TGet)!;
            if (typeof(TGet) == typeof(TerminalLogsViewModel)) return (Ui.TerminalLogsVm as TGet)!;
            if (typeof(TGet) == typeof(InspectorViewModel)) return (Ui.InspectorVm as TGet)!;
            if (typeof(TGet) == typeof(AdminViewModel)) return (Ui.AdminVm as TGet)!;
            if (typeof(TGet) == typeof(DeleteUsersViewModel)) return (Ui.DeleteUsersVm as TGet)!;
            if (typeof(TGet) == typeof(TitleBarViewModel)) return (Ui.TitleBarVm as TGet)!;
            if (typeof(TGet) == typeof(SeedUsersViewModel)) return (Ui.SeedUsersVm as TGet)!;
            if (typeof(TGet) == typeof(ContactsViewModel)) return (Ui.ContactsVm as TGet)!;
            if (typeof(TGet) == typeof(DeviceSettingsViewModel)) return (Ui.DeviceSettingsVm as TGet)!;
            if (typeof(TGet) == typeof(ProfileViewModel)) return (TGet)(object)Ui.ProfileVm;

            throw new ArgumentException($"Тип {typeof(TGet).Name} не найден в UI.");
        }
    }
}