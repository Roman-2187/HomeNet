using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Enums;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.ViewModels.MainZone;
using static HomeNetCore.Interfaces.ViewModels.AdminZone.IAdminVm;
using static HomeNetCore.Interfaces.ViewModels.MainZone.ITitleBarVm;

namespace HomeNetPresentation.ViewModels.AdminViews
{
    public partial class AdminViewModel : MainViews.FormViewModelBase,  IMenuViewModel
    {
        // Локальный стейт вкладки, скопированный из факта навигатора

        [ObservableProperty]
        public partial bool IsMenuOpen { get; set; } = false;

        [RelayCommand]
        private void ToggleMenu()
        {
            IsMenuOpen = !IsMenuOpen;
        }
        
         
        [RelayCommand]
        private void OpenProfile()
        {
           
            
        }


        // 🔥 Только закрывает (для кликов мимо)
        [RelayCommand]
        private void CloseMenu()
        {
            IsMenuOpen = false;
        }

        [ObservableProperty]
        public partial LogLevelFilter CurrentLogFilter { get; set; } = LogLevelFilter.All;



        [ObservableProperty]
        public partial AdminSubTab ActiveTab { get; set; } = AdminSubTab.None;


        // Тексты кнопок читают наше локальное свойство
        public string TableButtonText => ActiveTab == AdminSubTab.UserTable ? "Скрыть users 🙈" : "Показать users 👁️";
        public string ToggleButtonText => ActiveTab == AdminSubTab.Logger ? "Скрыть лог" : "Показать лог";
        public string DeleteButtonText => ActiveTab == AdminSubTab.DeleteUsers ? "Закрыть удаление" : "Удалить юзера";
        public string SeedButtonText => ActiveTab == AdminSubTab.SeedUsers ? "Закрыть сидинг" : "Сидинг Users";
        public string InspectorButtonText => ActiveTab == AdminSubTab.EventInspector ? "Закрыть граф" : "Инспектор шины";

        public AdminViewModel(IEventBus eventBus,ICastomLogger logger) : base(eventBus,logger)
            
            
            
        {
            // 🔥 ЧИСТОТА ДЛЯ ИНСПЕКТОРА: Ловим факт изменения вкладки именованным методом!
            EventBus.Subscribe<AdminTabChanged>(OnAdminTabChanged);
        }

       

        private void OnAdminTabChanged(AdminTabChanged? msg)
        {
            if (msg == null) return;

            // 🔥 Пишем в генерируемое Toolkit-ом свойство (с большой буквы)
            ActiveTab = msg.ActiveTab;



            var shouldBeWide = msg.ActiveTab is AdminSubTab.EventInspector or AdminSubTab.Logger or AdminSubTab.UserTable;

            // Лениво пуляем приказ нашему сервису трансформации окна
            EventBus.Publish(this, new ToggleWidescreen(shouldBeWide));

            NotifyAllButtonsChanged();
        }

       

        private void NotifyAllButtonsChanged()
        {
            OnPropertyChanged(nameof(TableButtonText));
            OnPropertyChanged(nameof(ToggleButtonText));
            OnPropertyChanged(nameof(DeleteButtonText));
            OnPropertyChanged(nameof(SeedButtonText));
            OnPropertyChanged(nameof(InspectorButtonText));
        }
        

        [RelayCommand]
        private void ShowInspector() =>EventBus.Publish(this, new OpenEventInspector());
       
        [RelayCommand]
        private void ShowUserTable() => EventBus.Publish(this, new OpenTableUsers());

        [RelayCommand]
        private void ShowLogPanel() => EventBus.Publish(this, new OpenLogger());

        [RelayCommand]
        private void ShowDeleteForm() => EventBus.Publish(this, new OpenDelete());

        [RelayCommand]
        private void ShowSeedForm() => EventBus.Publish(this, new OpenSeedUsers());

        [RelayCommand]
        private void Logout()
        {
            IsMenuOpen = false; 
            EventBus.Publish(this, new LogoutAdmin());
        }

        [RelayCommand]
        private void SwitchLogFilter(LogLevelFilter targetFilter) => CurrentLogFilter = targetFilter;


        public override void Dispose()
        {
            base.Dispose();
            EventBus.Unsubscribe<AdminTabChanged>(OnAdminTabChanged);
        }
    }
}
