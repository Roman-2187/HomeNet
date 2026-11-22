using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.Services;
using System.Collections.ObjectModel;
using static HomeNetCore.Interfaces.ViewModels.AdminZone.IAdminVm;
using static HomeNetCore.Interfaces.ViewModels.AdminZone.IUsersTableVm;
using static HomeNetOrm.Interfaces.Repositories.IUserRepo;

namespace HomeNetPresentation.ViewModels.AdminViews
{
    public partial class DeleteUsersViewModel : MainViews.FormViewModelBase
    {
        private readonly IDeleteService _srv;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
        public partial ObservableCollection<UserEntity> FoundUsers { get; set; } = [];

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
        public partial UserEntity? SelectedUser { get; set; }
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
        public partial string TargetUserEmail { get; set; } = string.Empty;
        public ObservableCollection<string> DeletedUsersHistory { get; } = [];

        public DeleteUsersViewModel(IDeleteService srv, IEventBus bus, ICastomLogger log)
            : base(bus,log)
        {
            _srv = srv ?? throw new ArgumentNullException(nameof(srv));
           
            SubmitButtonText = "Удалить";

            EventBus.Subscribe<AdminTabChanged>(OnAdminTabChanged);
            EventBus.Subscribe<Refreshed>(OnUsersRefreshed);

            // 🔥 ПОДПИСКА НА ЕДИНЫЙ ИСТОЧНИК ПРАВДЫ ОТ ОРМ
            EventBus.Subscribe<Deleted>(OnUserDeleted);

            ResetForm();
        }


        private void OnAdminTabChanged(AdminTabChanged msg)
        {
            if (msg?.ActiveTab != AdminSubTab.DeleteUsers)
            {
                return;
            }

            ResetForm();
            EventBus.Publish(this, new RefreshRequest());
        }

        private void OnUsersRefreshed(Refreshed? msg)
        {
            if (msg == null) return;
            FoundUsers = [.. msg.Users];
        }
        
        private void OnUserDeleted(Deleted msg)
        {
            var userToRemove = FoundUsers.FirstOrDefault(u => u.Id == msg.UserId);
            if (userToRemove != null)
            {
                FoundUsers.Remove(userToRemove);
            }
        }

        

        [RelayCommand(CanExecute = nameof(CanSearch))]
        private async Task SearchAsync()
        {
            StatusMessage = "Поиск...";
            var verdict = await _srv.SearchUserAsync(TargetUserEmail.Trim());
            StatusMessage = verdict.Results.FirstOrDefault()?.Message ?? string.Empty;

            if (verdict is { IsValid: true, FoundUser: not null })
            {
                if (FoundUsers.All(u => u.Id != verdict.FoundUser.Id))
                {
                    FoundUsers.Add(verdict.FoundUser);
                }
                SelectedUser = verdict.FoundUser;
            }
        }

        [RelayCommand]
        private async Task ExecuteDeleteAsync()
        {
            try
            {
                StatusMessage = "Удаление...";

                var verdict = await _srv.DeleteUserAsync(TargetUserEmail.Trim(), SelectedUser);
                UpdateValidation(verdict.Results);
                StatusMessage = verdict.Results.FirstOrDefault()?.Message ?? string.Empty;

                if (verdict.IsValid)
                {

                    if (!string.IsNullOrEmpty(verdict.HistoryMessage))
                    {
                        DeletedUsersHistory.Add(verdict.HistoryMessage);
                    }

                    await Task.Delay(500);
                    ResetForm();
                    EventBus.Publish(this, new RefreshRequest());
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }
        }

        [RelayCommand]
        private Task CancelAsync()
        {
            try
            {
                ResetForm();
                return Task.CompletedTask;
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }
        }


        private void ResetForm()
        {
            TargetUserEmail = string.Empty;
            SelectedUser = null;
            OnPropertyChanged(nameof(TargetUserEmail));
            OnPropertyChanged(nameof(SelectedUser));
            StatusMessage = "Введите Email или выберите пользователя";
        }

        private bool CanSearch()
        {
            if (FoundUsers.Count == 0 && string.IsNullOrWhiteSpace(TargetUserEmail)) return false;
            if (SelectedUser == null) return true;
            return !string.Equals(SelectedUser.Email?.Trim(), TargetUserEmail.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        partial void OnSelectedUserChanged(UserEntity? value)
        {
            if (value != null && !string.Equals(TargetUserEmail, value.Email, StringComparison.OrdinalIgnoreCase))
            {
                TargetUserEmail = value.Email ?? string.Empty;
                OnPropertyChanged(nameof(TargetUserEmail));
            }

            StatusMessage = value != null
                ? $"Выбран: {value.Email} — {value.FirstName} {value.LastName}"
                : $"Всего в базе: {FoundUsers.Count}. Выберите юзера.";
        }

        partial void OnTargetUserEmailChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                SelectedUser = null;
                OnPropertyChanged(nameof(SelectedUser));
                return;
            }

            var match = FoundUsers.FirstOrDefault(u => string.Equals(u.Email?.Trim(), value.Trim(), StringComparison.OrdinalIgnoreCase));
            SelectedUser = match;
            OnPropertyChanged(nameof(SelectedUser));

            if (match == null)
                StatusMessage = $"Введён сторонний Email: {value}. Нажмите 'Поиск'.";
        }
        

        public override void Dispose()
        {
            base.Dispose();
            EventBus.Unsubscribe<AdminTabChanged>(OnAdminTabChanged);
            EventBus.Unsubscribe<Refreshed>(OnUsersRefreshed);
            EventBus.Unsubscribe<Deleted>(OnUserDeleted);
        }
    }
}
