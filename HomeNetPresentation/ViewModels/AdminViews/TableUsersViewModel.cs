using CommunityToolkit.Mvvm.ComponentModel;
using HomeNet.Infrastructure.Shared;
using HomeNet.Infrastructure.Shared.Enums;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNetCore.Interfaces.ViewModels.AdminZone;
using HomeNetCore.Interfaces.ViewModels.MainZone;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Interfaces.Repositories;
using System.Collections.ObjectModel;
using AdminNavigationManager = HomeNetPresentation.Serviсes.AdminNavigationManager;


   namespace HomeNetPresentation.ViewModels.AdminViews
{
    /// <summary>
    /// Административная таблица пользователей SiberNet.
    /// Благодаря сквозному конвейеру бутстраппера вычищена от локов и потоковых костылей! 🧼
    /// </summary>
    public partial class TableUsersViewModel : FormViewModelBase<AdminNavigationManager>
    {
        private readonly IUserRepo _userRepo;
        private readonly ICastomLogger? _logger;
        private readonly SynchronizationContext? _uiContext = SynchronizationContext.Current;

        [ObservableProperty]
        public partial ObservableCollection<UserEntity> Users { get; set; } = [];

        public TableUsersViewModel(
            IEventBus eventBus,
            IUserRepo userRepo,
            AdminNavigationManager navigation,
            ICastomLogger? logger = null)
            : base(eventBus, navigation)
        {
            _userRepo = userRepo;
            _logger = logger;

            _eventBus.Subscribe<IUserRepo.Deleted>(OnUserDeletedFromDb);
            _eventBus.Subscribe<IUserRepo.Inserted>(OnUserAdded);
            _eventBus.Subscribe<IUserRepo.Updated>(OnUserUpdated);
            _eventBus.Subscribe<IUsersTableVm.RefreshRequest>(OnRefreshRequest);
            _eventBus.Subscribe<IDbContextContainer.InfrastructureReady>(OnInfrastructureReady);
        }

        private void OnInfrastructureReady(IDbContextContainer.InfrastructureReady msg) =>
            _ = AutoExceptionHandler.ExecuteSafeAsync(_logger, InitializeDataAsync);

        private void OnRefreshRequest(IUsersTableVm.RefreshRequest msg) =>
            _ = AutoExceptionHandler.ExecuteSafeAsync(_logger, InitializeDataAsync);

        private async Task InitializeDataAsync()
        {
            try
            {
                var list = await _userRepo.GetAllAsync();
                RunOnUi(() =>
                {
                    Users = [.. list ?? []];
                    BroadcastRefreshed();
                });
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, $"Ошибка старта таблицы: {ex.Message}");
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"Ошибка старта таблицы: {ex.Message}"));
            }
        }

        private void OnUserUpdated(IUserRepo.Updated? msg)
        {
            if (msg?.User == null) return;

            RunOnUi(() =>
            {
                var existingUser = Users.FirstOrDefault(u => u.Id == msg.User.Id);
                if (existingUser == null) return;

                var index = Users.IndexOf(existingUser);
                if (index < 0) return;

                Users[index] = msg.User;
                BroadcastRefreshed();
            });
        }

        private void OnUserDeletedFromDb(IUserRepo.Deleted? msg)
        {
            if (msg == null) return;

            RunOnUi(() =>
            {
                var uiUser = Users.FirstOrDefault(u => u.Id == msg.UserId);
                if (uiUser == null) return;

                Users.Remove(uiUser);
                BroadcastRefreshed();
            });
        }

        private void OnUserAdded(IUserRepo.Inserted? msg)
        {
            if (msg?.User == null) return;

            RunOnUi(() =>
            {
                if (Users.Any(u => u.Id == msg.User.Id)) return;

                Users.Add(msg.User);
                BroadcastRefreshed();
            });
        }

        private void RunOnUi(Action action)
        {
            if (_uiContext != null && SynchronizationContext.Current != _uiContext)
                _uiContext.Post(_ => action(), null);
            else
                action();
        }

        private void BroadcastRefreshed() =>
            _eventBus.Publish(this, new IUsersTableVm.Refreshed([.. Users]));

        public override void Dispose()
        {
            _eventBus.Unsubscribe<IUserRepo.Deleted>(OnUserDeletedFromDb);
            _eventBus.Unsubscribe<IUserRepo.Inserted>(OnUserAdded);
            _eventBus.Unsubscribe<IUserRepo.Updated>(OnUserUpdated);
            _eventBus.Unsubscribe<IUsersTableVm.RefreshRequest>(OnRefreshRequest);
            _eventBus.Unsubscribe<IDbContextContainer.InfrastructureReady>(OnInfrastructureReady);
            base.Dispose();
        }
    }
}


