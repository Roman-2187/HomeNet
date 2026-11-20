using CommunityToolkit.Mvvm.ComponentModel;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Interfaces.ViewModels.AdminZone;
using HomeNetCore.Interfaces.ViewModels.MainZone;
using HomeNetPresentation.Services;
using static HomeNet.Network.Handlers.ConnectionStatusHandler;

namespace HomeNetPresentation.ViewModels
{
    public partial class StatusBarViewModel : FormViewModelBase<BaseNavigationState>, IDisposable
    {

        private readonly SynchronizationContext? _uiContext = SynchronizationContext.Current;
        [ObservableProperty]
        private string _statusText = "Готов к работе";

        [ObservableProperty]
        private string _combinedStatusText = "OFFLINE";

        
        [ObservableProperty]
        private string _statusColor = "#FFF5546F"; // Стартуем с тревожного розово-красного

        public StatusBarViewModel(IEventBus eventBus, BaseNavigationState navigation)
            : base(eventBus, navigation)
        {
            InitializeBusSubscriptions();
        }

        private void InitializeBusSubscriptions()
        {
            _eventBus.Subscribe<IStatusBarViewModel.TextChanged>(OnTextChanged);
            _eventBus.Subscribe<IUsersTableVm.Refreshed>(OnUsersTableRefreshed);
            _eventBus.Subscribe<ServerStatusChangedEvent>(OnServerStatusChanged);

        }


        private void OnServerStatusChanged(ServerStatusChangedEvent msg)
        {
            if (msg == null) return;

            void UpdateUi()
            {
                if (msg.IsOnline)
                {
                    CombinedStatusText = "ONLINE";
                    StatusColor = "#FF4CAF50";
                }
                else
                {
                    CombinedStatusText = "OFFLINE";
                    StatusColor = "#FFF5546F";
                }
            }

            // Если ViewModel создана в UI-потоке — маршалим вызов туда через стандартный .NET
            // Если в юнит-тесте (_uiContext == null) — выполняем напрямую
            if (_uiContext != null)
            {
                _uiContext.Post(_ => UpdateUi(), null);
            }
            else
            {
                UpdateUi();
            }
        }


        private async void OnTextChanged(IStatusBarViewModel.TextChanged msg)
        {
            if (msg == null) return;
            await UpdateStatusTextAsync(msg.NewStatus);
        }

        private void OnUsersTableRefreshed(IUsersTableVm.Refreshed msg)
        {
            if (msg == null) return;

            StatusText = msg.Users != null && msg.Users.Count > 0
                ? $"База данных синхронизирована. Загружено пользователей: {msg.Users.Count}"
                : "Синхронизация завершена. Список пользователей пуст.";
        }

       

        private Task UpdateStatusTextAsync(string newText)
        {
            StatusText = newText;
            return Task.CompletedTask;
        }

        #region 🛡️ ПУЛЕНЕПРОБИВАЕМЫЙ СТЕРИЛИЗАТОР ПАМЯТИ

        public override void Dispose()
        {
            base.Dispose();
            _eventBus.Unsubscribe<IStatusBarViewModel.TextChanged>(OnTextChanged);
            _eventBus.Unsubscribe<IUsersTableVm.Refreshed>(OnUsersTableRefreshed);
         
        }

        #endregion
    }
}
