using CommunityToolkit.Mvvm.ComponentModel;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using static HomeNet.Network.Handlers.ConnectionStatusHandler;
using static HomeNetCore.Interfaces.ViewModels.AdminZone.IUsersTableVm;
using static HomeNetCore.Interfaces.ViewModels.MainZone.IStatusBarViewModel;

namespace HomeNetPresentation.ViewModels.MainViews
{
    public partial class StatusBarViewModel : FormViewModelBase
    {
        private readonly SynchronizationContext? _uiContext = SynchronizationContext.Current;

        [ObservableProperty]
        public partial string StatusText { get; set; } = "Готов к работе";

        [ObservableProperty]
        public partial string CombinedStatusText { get; set; } = "OFFLINE";

        [ObservableProperty]
        public partial string StatusColor { get; set; } = "#FFF5546F";

        public StatusBarViewModel(IEventBus eventBus,ICastomLogger logger) : base(eventBus, logger)
        {
            EventBus.Subscribe<TextChanged>(OnTextChanged);
            EventBus.Subscribe<Refreshed>(OnUsersTableRefreshed);
            EventBus.Subscribe<ServerStatusChangedEvent>(OnServerStatusChanged);
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

        private void OnTextChanged(TextChanged? msg)
        {
            if (msg == null) return;
            StatusText = msg.NewStatus;
        }

        private void OnUsersTableRefreshed(Refreshed? msg)
        {
            if (msg == null) return;

            StatusText = msg.Users.Count > 0
                ? $"База данных синхронизирована. Загружено пользователей: {msg.Users.Count}"
                : "Синхронизация завершена. Список пользователей пуст.";
        }

        #region 🛡️ ПУЛЕНЕПРОБИВАЕМЫЙ СТЕРИЛИЗАТОР ПАМЯТИ

        public override void Dispose()
        {
            EventBus.Unsubscribe<TextChanged>(OnTextChanged);
            EventBus.Unsubscribe<Refreshed>(OnUsersTableRefreshed);
            EventBus.Unsubscribe<ServerStatusChangedEvent>(OnServerStatusChanged);
            base.Dispose();
        }

        #endregion
    }
}

