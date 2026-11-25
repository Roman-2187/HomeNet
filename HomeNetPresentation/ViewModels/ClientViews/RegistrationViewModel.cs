using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Validation;
using HomeNetPresentation.ViewModels.MainViews;
using static HomeNetCore.Enums.TypeField;
using static HomeNetCore.Enums.ValidationState;
using static HomeNetCore.Interfaces.ViewModels.UserZone.IUserVm;

namespace HomeNetPresentation.ViewModels.ClientViews
{
    /// <summary>
    /// Чистая форма регистрации мессенджера HomeNet.
    /// </summary>
    public sealed partial class RegistrationViewModel : FormViewModelBase
    {
        private readonly IRegistrationService _registerService;

        [ObservableProperty]
        public partial UserEntity UserData { get; set; } = new();

        public RegistrationViewModel(
            IRegistrationService registerService,
            IEventBus eventBus,
            ICastomLogger logger) : base(eventBus, logger)
        {
            _registerService = registerService ?? throw new ArgumentNullException(nameof(registerService));
            InitializeInitialHints();
        }

        [RelayCommand]
        private async Task CheckEmailAsync()
        {
            var email = UserData.Email;
            if (string.IsNullOrWhiteSpace(email)) return;

            SetFieldValidation(new ValidationResult
            {
                Field = EmailType,
                State = Info,
                Message = "Проверка..."
            });

            var result = await _registerService.CheckEmailOnBlurAsync(email);
            SetFieldValidation(result);
        }

        [RelayCommand]
        private async Task RegisterAsync()
        {
            StatusMessage = string.Empty;

            try
            {
                var verdict = await _registerService.RegisterUserAsync(UserData);
                UpdateValidation(verdict.Results);

                if (verdict.IsValid)
                    await CompleteRegistrationAsync(verdict.VerifiedUser ?? UserData);
                else
                    HandleValidationFailure();
            }
            catch (Exception ex)
            {
                StatusMessage = $"При регистрации произошла ошибка: {ex.Message}";
                Logger.LogError($"[RegisterVM] Критический сбой регистрации: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            ResetForm();
            InitializeInitialHints();
            EventBus.Publish(this, new BackToStart());
        }

        private void ResetForm()
        {
            UserData = new UserEntity();
            StatusMessage = string.Empty;
            ClearValidation();
        }

        private async Task CompleteRegistrationAsync(UserEntity finalUser)
        {
            StatusMessage = "Вы успешно зарегистрированы";
            SubmitButtonText = "Готово!";
            Logger.LogInfo($"[RegisterVM] Пользователь {finalUser.Email} успешно прошёл СУБД.");

            EventBus.Publish(this, new UserSignedUp(finalUser));

            await Task.Delay(500);
            ResetForm();
            InitializeInitialHints();
        }

        private void HandleValidationFailure()
        {
            StatusMessage = "Есть ошибки в полях";
            Logger.LogWarning($"[RegisterVM] Ошибка заполнения полей регистрации для: {UserData.Email}");
        }

        private void InitializeInitialHints()
        {
            UpdateValidation(
            [
                new(EmailType, "Введите email например 'User@example.com'", Info, true),
                new(PasswordType, "Пароль должен содержать 8 символов буквы и цифры", Info, true),
                new(NameType, "Имя пользователя должно содержать 3 буквы подряд", Info, true),
                new(ConfirmedPasswordType, "Пароли должны совпадать", Info, true)
            ]);
            SubmitButtonText = "Создать ID";
        }
    }
}
