using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Validation;
using HomeNetPresentation.ViewModels.MainViews;
using static HomeNetCore.Interfaces.ViewModels.UserZone.IUserVm;

namespace HomeNetPresentation.ViewModels.ClientViews
{
    /// <summary>
    /// Стерильная форма авторизации мессенджера HomeNet.
    /// </summary>
    public sealed partial class AuthenticationViewModel : FormViewModelBase
    {
        private readonly IAuthenticateService _loginService;

        [ObservableProperty]
        public partial UserEntity UserData { get; set; } = new();

        public AuthenticationViewModel(
            IAuthenticateService loginService,
            IEventBus eventBus,
            ICastomLogger logger) : base(eventBus, logger)
        {
            _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
            InitializeInitialHints();
        }

        private void InitializeInitialHints()
        {
            UpdateValidation(
            [
                new(TypeField.EmailType, "Введите email", ValidationState.Info, true),
                new(TypeField.PasswordType, "Текущий пароль", ValidationState.Info, true)
            ]);
            StatusMessage = string.Empty;
        }

        private void ResetForm()
        {
            UserData = new UserEntity();
            StatusMessage = string.Empty;
            ClearValidation();
            SubmitButtonText = "войти";
        }

        [RelayCommand]
        private async Task CheckEmailAsync()
        {
            var email = UserData.Email;
            if (string.IsNullOrWhiteSpace(email)) return;

            SetFieldValidation(new ValidationResult
            {
                Field = TypeField.EmailType,
                State = ValidationState.Info,
                Message = "Проверка..."
            });

            var result = await _loginService.CheckEmailOnBlurAsync(email);
            SetFieldValidation(result);
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            StatusMessage = string.Empty;
            ClearValidation();

            try
            {
                var verdict = await _loginService.CheckUserAsync(UserData);
                UpdateValidation(verdict.Messages);

                if (verdict.IsSuccess)
                {
                    StatusMessage = "Вход выполнен успешно";
                    SubmitButtonText = "Входим...";
                    Logger.LogInfo($"[AuthVM] Пользователь {verdict.User?.Email ?? UserData.Email} успешно авторизован.");

                    var finalUser = verdict.User ?? UserData;
                    EventBus.Publish(this, new UserSignedIn(finalUser));

                    await Task.Delay(500);
                    ResetForm();
                    InitializeInitialHints();
                }
                else
                {
                    StatusMessage = "Есть ошибки в полях";
                    Logger.LogWarning($"[AuthVM] Неудачная попытка входа для: {UserData.Email}");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"При входе произошла ошибка: {ex.Message}";
                Logger.LogError($"[AuthVM] Критический сбой авторизации: {ex.Message}");
            }
        }

        [RelayCommand]
        private void OpenRegisterZone()
        {
            Logger.LogInfo("[AuthVM] Запрос перехода на экран регистрации через EventBus.");
            EventBus.Publish(this, new OpenRegistration());
        }

        [RelayCommand]
        private void Cancel()
        {
            ResetForm();
            InitializeInitialHints();
            EventBus.Publish(this, new BackToStart());
        }
    }
}
