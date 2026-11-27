using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Interfaces.ViewModels.UserZone;

using HomeNetPresentation.Serviсes;
using HomeNetPresentation.ViewModels.AdminViews;
using HomeNetPresentation.ViewModels.ClientViews;
using HomeNetPresentation.ViewModels.ClientViews.Chat;
using HomeNetPresentation.ViewModels.MainViews;

namespace HomeNet.DI.Modules
{
    public class PresentationUiModule
    {
        public BaseNavigationState Navigation { get; }
        public AdminNavigationManager AdminNavigation { get; }
        public UserNavigationManager UserNavigation { get; }

        public StatusBarViewModel StatusBarVm { get; }
        public SystemButtonsViewModel SystemButtonsVm { get; }
        public RegistrationViewModel RegistrationVm { get; }
        public AuthenticationViewModel AuthenticationVm { get; }
        public TerminalLogsViewModel TerminalLogsVm { get; }
        public InspectorViewModel InspectorVm { get; }
        public AdminViewModel AdminVm { get; }
        public DeleteUsersViewModel DeleteUsersVm { get; }
        public ProfileViewModel ProfileVm { get; }
        public ChatViewModel ChatVm { get; }
        public TitleBarViewModel TitleBarVm { get; }
        public SeedUsersViewModel SeedUsersVm { get; }
        public TableUsersViewModel TableUsersVm { get; }
        public IUserVm UserVm { get; }
        public ContactsViewModel ContactsVm { get; }
        public MainViewModel MainVm { get; }

        // 🔥 НАША НОВАЯ ВЬЮМОДЕЛЬ НАСТРОЕК ЖЕЛЕЗА
        public DeviceSettingsViewModel DeviceSettingsVm { get; }

       
        public PresentationUiModule(
            IEventBus eventBus,
            ICastomLogger logger,
            BusinessServicesModule services, // 🔥 ОДИН ОБЪЕКТ Вместо 6 разрозненных сервисов!
            IEventInspector inspector)
        {
            Navigation = new BaseNavigationState(eventBus);
            AdminNavigation = new AdminNavigationManager(eventBus);
            UserNavigation = new UserNavigationManager(eventBus, logger);
            StatusBarVm = new StatusBarViewModel(eventBus,logger);
            SystemButtonsVm = new SystemButtonsViewModel(eventBus);

            // Вытаскиваем сервисы из контейнера через точку — строго, типизировано и красиво!
            RegistrationVm = new RegistrationViewModel(services.RegistrationService, eventBus, logger);
            AuthenticationVm = new AuthenticationViewModel(services.AuthenticateService, eventBus, logger);
            TerminalLogsVm = new TerminalLogsViewModel(eventBus);
            InspectorVm = new InspectorViewModel(eventBus, logger, inspector);
            AdminVm = new AdminViewModel(eventBus,logger);
            DeleteUsersVm = new DeleteUsersViewModel(services.DeleteService, eventBus, logger);
            ChatVm = new ChatViewModel(eventBus, services.MessageService);
            TitleBarVm = new TitleBarViewModel(eventBus, logger);
            SeedUsersVm = new SeedUsersViewModel(eventBus,logger, services.UserRepository);
            TableUsersVm = new TableUsersViewModel(eventBus,logger, services.UserRepository);
            UserVm = new UserViewModel(logger, eventBus);
            ProfileVm = new ProfileViewModel(eventBus, services.ProfileService); 
            DeviceSettingsVm = new DeviceSettingsViewModel(eventBus, logger,services.DeviceSettingsService, services.UserRepository);
            ContactsVm = new ContactsViewModel(eventBus);
            MainVm = new MainViewModel(logger, eventBus);
        }
    }
}
