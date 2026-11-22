using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Enums.Navigation;
using static HomeNetCore.Interfaces.ViewModels.AdminZone.IAdminVm;
using static HomeNetCore.Interfaces.ViewModels.MainZone.IStatusBarViewModel;
using static HomeNetCore.Interfaces.ViewModels.MainZone.ITitleBarVm;
using static HomeNetCore.Interfaces.ViewModels.UserZone.IUserVm;

namespace HomeNetPresentation.ViewModels.MainViews
{
    /// <summary>
    /// Стерильная верхняя панель управления окном мессенджера HomeNet.
    /// Полная изоляция: класс общается с навигацией ИСКЛЮЧИТЕЛЬНО через рацию автобуса.
    /// </summary>
    public partial class TitleBarViewModel : FormViewModelBase
    {
        [ObservableProperty]
        public partial MainTab CurrentMainZone { get; set; } = MainTab.StartZone;

        [ObservableProperty]
        public partial ClientSubTab CurrentClientTab { get; set; } = ClientSubTab.None;

        public TitleBarViewModel(IEventBus eventBus,ICastomLogger logger) : base(eventBus, logger)
        {
            EventBus.Subscribe<MacroZoneChanged>(OnMacroZoneChanged);
        }

        #region 🎧 МЕТОД ПРИЁМА ОБРАТНОГО СИГНАЛА БЭКЕНДА 🧼

        private void OnMacroZoneChanged(MacroZoneChanged? msg)
        {
            if (msg == null) return;

            CurrentMainZone = msg.ActiveZone;
            CurrentClientTab = msg.ActiveClientTab;
        }

        #endregion

        // 🛠️ КНОПКА: Админка
        [RelayCommand]
        private void ToggleAdminZone()
        {
            if (CurrentMainZone != MainTab.AdminZone)
            {
                EventBus.Publish(this, new RequestAdminZone());
                
                Logger.LogInfo("нажата админка");
            }
            else
            {
                EventBus.Publish(this, new LogoutAdmin());
            }
        }

        // 🔑 КНОПКА: Вход
        [RelayCommand]
        private void OpenAuthZone() => EventBus.Publish(this, new OpenAuth());

        // 📝 КНОПКА: Регистрация
        [RelayCommand]
        private void OpenRegisterZone() => EventBus.Publish(this, new OpenRegistration());
      

        // 🔑 КНОПКА: Выход из профиля клиента
        [RelayCommand]
        private void Logout()
        {
            EventBus.Publish(this, new LogoutClient());
            EventBus.Publish(this, new TextChanged("Выход из аккаунта выполнен успешно"));
        }
        

        public override void Dispose()
        {
            base.Dispose();
            EventBus.Unsubscribe<MacroZoneChanged>(OnMacroZoneChanged);
        }

      
    }
}
