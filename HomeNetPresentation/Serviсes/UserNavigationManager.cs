using CommunityToolkit.Mvvm.ComponentModel;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.ViewModels.MainZone;
using static HomeNetCore.Interfaces.ViewModels.UserZone.IContactsVm;
using static HomeNetCore.Interfaces.ViewModels.UserZone.IUserVm;

namespace HomeNetPresentation.Serviсes
{
    /// <summary>
    /// Автономный менеджер навигации зоны пользователя мессенджера SiberNet.
    /// Рулит гостевыми экранами, мессенджером и открытием чатов через шину событий. 🦾
    /// </summary>
    public partial class UserNavigationManager : BaseNavigationState
    {
        private ICastomLogger Logger { get; set; }
        [ObservableProperty] public partial ClientSubTab CurrentClientTab { get; set; } = ClientSubTab.None;

        public UserNavigationManager(IEventBus eventBus, ICastomLogger logger) : base(eventBus)
        {
            Logger = logger;
            EventBus.Subscribe<OpenAuth>(OnOpenAuthRequested);
            EventBus.Subscribe<OpenRegistration>(OnOpenRegistrationRequested);
            EventBus.Subscribe<OpenProfile>(OnOpenProfileRequested);
            EventBus.Subscribe<BackToStart>(OnBackToStartRequested);

            // Подписки на успех авторизации/регистрации
            EventBus.Subscribe<UserSignedIn>(OnUserAuthenticatedRequested);
            EventBus.Subscribe<UserSignedUp>(OnUserSignedUpRequested);

            // 🔥 ЛОВИМ КЛИК ПО ПОЛЬЗОВАТЕЛЮ ИЗ СПИСКА КОНТАКТОВ!
            EventBus.Subscribe<FriendSelected>(OnFriendSelectedRequested);

            EventBus.Subscribe<LogoutClient>(OnLogoutClientRequested);
        }

       

        private void OnOpenAuthRequested(OpenAuth msg)
        {
            CurrentMainZone = MainTab.ClientZone;
            CurrentClientTab = ClientSubTab.Authentication;
            PublishStateDuet();

            Logger.LogInfo($"Зашли в OnOpenAuthRequested текущий стате {CurrentClientTab}");
        }

        private void OnOpenRegistrationRequested(OpenRegistration msg)
        {
            CurrentMainZone = MainTab.ClientZone;
            CurrentClientTab = ClientSubTab.Registration;
            PublishStateDuet();

            Logger.LogInfo($"Зашли в OnOpenRegistrationRequested текущий стате {CurrentClientTab}");
        }
        
        
        // Метод-обработчик:
        private void OnOpenProfileRequested(OpenProfile msg)
        {
            if (CurrentUser == null) return; // На всякий случай защита, если не авторизован

            CurrentMainZone = MainTab.ClientZone;
            CurrentClientTab = ClientSubTab.Profile;
            PublishStateDuet();

            Logger.LogInfo($"[Navigator] Открыт профиль пользователя: {CurrentUser.FirstName}");
        }

        private void OnBackToStartRequested(BackToStart msg)
        {
            CurrentMainZone = MainTab.StartZone;
            CurrentClientTab = ClientSubTab.None;
            PublishStateDuet();
        }

        private void OnUserAuthenticatedRequested(UserSignedIn msg)
        {
            if (msg?.User == null) return;

            CurrentUser = msg.User;
            CurrentMainZone = MainTab.ClientZone;
            CurrentClientTab = ClientSubTab.Messenger;
            PublishStateDuet();
        }

        private void OnUserSignedUpRequested(UserSignedUp msg)
        {
            if (msg?.User == null) return;

            CurrentUser = msg.User;
            CurrentMainZone = MainTab.ClientZone;
            CurrentClientTab = ClientSubTab.Messenger;
            PublishStateDuet();
        }

        /// <summary>
        /// 🔥 РЕАКЦИЯ НА КЛИК: Переключаем энум на ЧАТ, когда выбрали друга
        /// </summary>
        private void OnFriendSelectedRequested(FriendSelected msg)
        {
            if (msg?.Friend == null) return;

            Logger.LogInfo($"[Navigator] Выбран контакт: {msg.Friend.FirstName}. Переключаем стейт на Chat.");

            // Переводим под-вкладку в режим чата, чтобы XAML скрыл заглушку и открыл ChatViewControl!
            CurrentClientTab = ClientSubTab.Chat;
            PublishStateDuet();
        }

        private void OnLogoutClientRequested(LogoutClient msg)
        {
            CurrentUser = null;
            CurrentMainZone = MainTab.StartZone;
            CurrentClientTab = ClientSubTab.None;
            PublishStateDuet();
        }

       

        private void PublishStateDuet()
        {
            EventBus.Publish(this, new ITitleBarVm.MacroZoneChanged(CurrentMainZone, CurrentClientTab));
            EventBus.Publish(this, new ClientTabChanged(CurrentClientTab));
        }

        public override void Dispose()
        {
            base.Dispose();
            EventBus.Unsubscribe<OpenAuth>(OnOpenAuthRequested);
            EventBus.Unsubscribe<OpenRegistration>(OnOpenRegistrationRequested);
            EventBus.Unsubscribe<BackToStart>(OnBackToStartRequested);
            EventBus.Unsubscribe<UserSignedIn>(OnUserAuthenticatedRequested);
            EventBus.Unsubscribe<UserSignedUp>(OnUserSignedUpRequested);

            // 🔥 Не забываем отписаться
            EventBus.Unsubscribe<FriendSelected>(OnFriendSelectedRequested);

            EventBus.Unsubscribe<LogoutClient>(OnLogoutClientRequested);
        }
    }
}
