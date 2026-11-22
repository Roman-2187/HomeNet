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


namespace HomeNetPresentation.ViewModels.AdminViews
{
    /// <summary>
    /// Административная таблица пользователей HomeNet.
    /// Благодаря сквозному конвейеру бутстраппера вычищена от локов и потоковых костылей! 🧼
    /// </summary>
    public partial class TableUsersViewModel : MainViews.FormViewModelBase
    {
        private readonly IUserRepo _userRepo;
        private readonly ICastomLogger? _logger;
        private readonly SynchronizationContext? _uiContext = SynchronizationContext.Current;

        [ObservableProperty]
        public partial ObservableCollection<UserEntity> Users { get; set; } = [];

        public TableUsersViewModel(IEventBus eventBus,ICastomLogger logger ,IUserRepo userRepo)
            : base(eventBus,logger)
        {
            _userRepo = userRepo;
            _logger = logger;

            EventBus.Subscribe<IUserRepo.Deleted>(OnUserDeletedFromDb);
            EventBus.Subscribe<IUserRepo.Inserted>(OnUserAdded);
            EventBus.Subscribe<IUserRepo.Updated>(OnUserUpdated);
            EventBus.Subscribe<IUsersTableVm.RefreshRequest>(OnRefreshRequest);
            EventBus.Subscribe<IDbContextContainer.InfrastructureReady>(OnInfrastructureReady);
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
                // ReSharper disable once MethodHasAsyncOverload
                EventBus.Publish(this, new IStatusBarViewModel.TextChanged($"Ошибка старта таблицы: {ex.Message}"));
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
            EventBus.Publish(this, new IUsersTableVm.Refreshed([.. Users]));

        public override void Dispose()
        {
            EventBus.Unsubscribe<IUserRepo.Deleted>(OnUserDeletedFromDb);
            EventBus.Unsubscribe<IUserRepo.Inserted>(OnUserAdded);
            EventBus.Unsubscribe<IUserRepo.Updated>(OnUserUpdated);
            EventBus.Unsubscribe<IUsersTableVm.RefreshRequest>(OnRefreshRequest);
            EventBus.Unsubscribe<IDbContextContainer.InfrastructureReady>(OnInfrastructureReady);
            base.Dispose();
        }
    }
}


