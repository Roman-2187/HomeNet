using CommunityToolkit.Mvvm.ComponentModel;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Interfaces.ViewModels.AdminZone;
using HomeNetCore.Interfaces.ViewModels.MainZone;
using static HomeNet.Network.Handlers.ConnectionStatusHandler;
using BaseNavigationState = HomeNetPresentation.Serviсes.BaseNavigationState;

namespace HomeNetPresentation.ViewModels
{
    public partial class StatusBarViewModel : FormViewModelBase<BaseNavigationState>
    {
        private readonly SynchronizationContext? _uiContext = SynchronizationContext.Current;

        [ObservableProperty]
        public partial string StatusText { get; set; } = "Готов к работе";

        [ObservableProperty]
        public partial string CombinedStatusText { get; set; } = "OFFLINE";

        [ObservableProperty]
        public partial string StatusColor { get; set; } = "#FFF5546F";

        public StatusBarViewModel(IEventBus eventBus, BaseNavigationState navigation)
            : base(eventBus, navigation)
        {
            _eventBus.Subscribe<IStatusBarViewModel.TextChanged>(OnTextChanged);
            _eventBus.Subscribe<IUsersTableVm.Refreshed>(OnUsersTableRefreshed);
            _eventBus.Subscribe<ServerStatusChangedEvent>(OnServerStatusChanged);
        }

        private void OnServerStatusChanged(ServerStatusChangedEvent? msg)
        {
            if (msg == null) return;

            if (_uiContext != null)
                _uiContext.Post(_ => UpdateUi(msg.IsOnline), null);
            else
                UpdateUi(msg.IsOnline);
        }

        private void UpdateUi(bool isOnline)
        {
            CombinedStatusText = isOnline ? "ONLINE" : "OFFLINE";
            StatusColor = isOnline ? "#FF4CAF50" : "#FFF5546F";
        }

        private void OnTextChanged(IStatusBarViewModel.TextChanged? msg)
        {
            if (msg == null) return;
            StatusText = msg.NewStatus;
        }

        private void OnUsersTableRefreshed(IUsersTableVm.Refreshed? msg)
        {
            if (msg == null) return;

            StatusText = msg.Users.Count > 0
                ? $"База данных синхронизирована. Загружено пользователей: {msg.Users.Count}"
                : "Синхронизация завершена. Список пользователей пуст.";
        }

        #region 🛡️ ПУЛЕНЕПРОБИВАЕМЫЙ СТЕРИЛИЗАТОР ПАМЯТИ

        public override void Dispose()
        {
            _eventBus.Unsubscribe<IStatusBarViewModel.TextChanged>(OnTextChanged);
            _eventBus.Unsubscribe<IUsersTableVm.Refreshed>(OnUsersTableRefreshed);
            _eventBus.Unsubscribe<ServerStatusChangedEvent>(OnServerStatusChanged);
            base.Dispose();
        }

        #endregion
    }
}

