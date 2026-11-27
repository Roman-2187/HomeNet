using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Interfaces.ViewModels.MainZone;
using static HomeNetCore.Interfaces.ViewModels.UserZone.IUserVm;

namespace HomeNetPresentation.ViewModels.ClientViews
{
    public sealed partial class ProfileViewModel : ObservableObject, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly IProfileService _profileService;
        private CancellationTokenSource? _confirmResetCts;

        [ObservableProperty]
        public partial UserEntity? CurrentUser { get; set; }

        [ObservableProperty]
        public partial bool IsConfirmingDelete { get; set; }

        [ObservableProperty]
        public partial string DeleteButtonText { get; set; } = "УДАЛИТЬ ПРОФИЛЬ";

        [ObservableProperty]
        public partial bool IsBusy { get; set; }

        public ProfileViewModel(IEventBus eventBus, IProfileService profileService)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));

            _eventBus.Subscribe<UserSignedIn>(OnUserSignedIn);
            _eventBus.Subscribe<UserSignedUp>(OnUserSignedUp);
            _eventBus.Subscribe<LogoutClient>(OnLogout);
        }

        private void OnUserSignedIn(UserSignedIn msg) => CurrentUser = msg?.User;
        private void OnUserSignedUp(UserSignedUp msg) => CurrentUser = msg?.User;

        private void OnLogout(LogoutClient msg)
        {
            ResetConfirmation();
            CurrentUser = null;
        }

        [RelayCommand]
        private void BackToMessenger()
        {
            ResetConfirmation();
            // Возвращаем пользователя в зону мессенджера (или через отдельное событие BackToMessenger)
            if (CurrentUser != null)
                _eventBus.Publish(this, new UserSignedIn(CurrentUser));
        }

        [RelayCommand]
        private async Task DeleteProfileAsync()
        {
            if (CurrentUser == null || IsBusy) return;

            // Шаг 1: Защита от случайного мисклика (даём 4 секунды на подтверждение)
            if (!IsConfirmingDelete)
            {
                IsConfirmingDelete = true;
                DeleteButtonText = "ТОЧНО УДАЛИТЬ?";
                StartConfirmationTimeout();
                return;
            }

            // Шаг 2: Реальное удаление
            ResetConfirmation();
            IsBusy = true;

            try
            {
                var verdict = await _profileService.DeleteAccountAsync(CurrentUser.Id);

                if (verdict.IsValid)
                {
                    _eventBus.Publish(this, new IStatusBarViewModel.TextChanged("[СИСТЕМА]: Профиль полностью удалён."));
                    // Выбрасываем на экран входа — все вьюмодели сами очистят свои стейты по LogoutClient
                    _eventBus.Publish(this, new LogoutClient());
                }
                else
                {
                    _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"[ОШИБКА ПРОФИЛЯ]: {verdict.ErrorMessage}"));
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void StartConfirmationTimeout()
        {
            _confirmResetCts?.Cancel();
            _confirmResetCts = new CancellationTokenSource();
            var token = _confirmResetCts.Token;

            try
            {
                await Task.Delay(4000, token);
                if (!token.IsCancellationRequested)
                    ResetConfirmation();
            }
            catch (TaskCanceledException) { }
        }

        private void ResetConfirmation()
        {
            _confirmResetCts?.Cancel();
            IsConfirmingDelete = false;
            DeleteButtonText = "УДАЛИТЬ ПРОФИЛЬ";
        }

        public void Dispose()
        {
            _confirmResetCts?.Cancel();
            _eventBus.Unsubscribe<UserSignedIn>(OnUserSignedIn);
            _eventBus.Unsubscribe<UserSignedUp>(OnUserSignedUp);
            _eventBus.Unsubscribe<LogoutClient>(OnLogout);
        }
    }
}
