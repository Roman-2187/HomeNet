using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.ViewModels.UserZone;
using static HomeNetCore.Interfaces.ViewModels.UserZone.IUserVm;

namespace HomeNetPresentation.ViewModels.ClientViews
{
    /// <summary>
    /// Тотально изолированная и чистая панель пользователя HomeNet.
    /// Больше не содержит внутри себя ссылки на другие вью-модели! 🧼
    /// </summary>
    public partial class UserViewModel : MainViews.FormViewModelBase,IUserVm
    {
        private readonly ICastomLogger _logger;

        [ObservableProperty]
        public partial ClientSubTab ActiveClientTab { get; set; } = ClientSubTab.None;

        public UserViewModel(ICastomLogger logger, IEventBus eventBus)  : base(eventBus,logger)
          
        {
            _logger = logger;
            InitializeBusSubscriptions();
        }

        

        private void InitializeBusSubscriptions()
        {      
            EventBus.Subscribe<ClientTabChanged>(OnClientTabChanged);       
        }

        
        [RelayCommand]
        private void LogoutDevice()
        {
            _logger.LogInfo("[UserVM] Пользователь инициировал глобальный выход из системы.");

            // Просто стреляем в шину. Мы никого не ждем, вьюмодель пользователя чиста!
            EventBus.Publish(this, new LogoutClient());
        }


        private void OnClientTabChanged(ClientTabChanged? msg)
        {
            if (msg == null)
            {
                _logger.LogError("[UserVM] OnClientTabChanged вызван, но сообщение msg == NULL!");
                return;
            }
            
            ActiveClientTab = msg.ActiveTab;
            OnPropertyChanged(nameof(ActiveClientTab));
        }
        
       

       

        public override void Dispose()
        {
            base.Dispose();
            EventBus.Unsubscribe<ClientTabChanged>(OnClientTabChanged);
        }
    }
}
