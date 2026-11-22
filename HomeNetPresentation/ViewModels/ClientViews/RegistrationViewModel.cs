using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNetCore.Enums;
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
    /// Наследуется от дженерик-базы с указанием конкретного менеджера пользователя.
    /// </summary>
    public partial class RegistrationViewModel(
        IRegistrationService registerService,
        IEventBus eventBus,
        ICastomLogger logger)
        : FormViewModelBase(eventBus, logger)
    {
        [ObservableProperty]
        // ReSharper disable once MemberCanBePrivate.Global
        public partial UserEntity UserData { get; set; } = new();

        
        [RelayCommand]
        private async Task CheckEmailAsync()
        {
            var email = UserData.Email;
            if (string.IsNullOrWhiteSpace(email)) return;

            // 🔥 1. СРАЗУ показываем статус "Проверка..."
            // Создаем временный результат
            var checkingResult = new ValidationResult
            {
                Field = EmailType,
                State = Info,
                Message = "Проверка..."
            };

            // Создаем новый словарь и кладем туда "Проверка..."
            var checkingDict = new Dictionary<TypeField, ValidationResult>(ValidationResults) { [EmailType] = checkingResult };
            ValidationResults = checkingDict;

            // 2. Дергаем сервис (он будет ждать ответа или таймаута)
            var result = await registerService.CheckEmailOnBlurAsync(email);

            // 3. Создаем ЕЩЕ ОДИН словарь уже с реальным результатом
            var finalDict = new Dictionary<TypeField, ValidationResult>(ValidationResults) { [EmailType] = result };
            ValidationResults = finalDict;
        }

        
        [RelayCommand]
        private async Task RegisterAsync()
        {
            StatusMessage = string.Empty;

            try
            {
                var verdict = await registerService.RegisterUserAsync(UserData);
                ApplyValidationResults(verdict.Results);

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
            // 🔥 ЧИСТОТА: Отправляем реактивный рекорд отмены. Навигатор сам вернет нас в абсолютный ноль гостя
            EventBus.Publish(this, new BackToStart());
        }
        
        
        private void ResetForm()
        {
            UserData = new UserEntity();
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();
        }

        
        private void ApplyValidationResults(IEnumerable<ValidationResult> results)
        {
            var list = results.ToList();
            ValidationResults = new Dictionary<TypeField, ValidationResult>();

            UpdateValidation([]);

            ValidationResults = list.ToDictionary(r => r.Field, r => r);
            UpdateValidation(list);
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
            var initialHints = new List<ValidationResult>
            {
                new(EmailType, "Введите email например 'User@example.com'", Info, true),
                new(PasswordType, "Пароль должен содержать 8 символов буквы и цифры", Info, true),
                new(NameType, "Имя пользователя должно содержать 3 буквы подряд", Info, true),
                new(ConfirmedPasswordType, "Пароли должны совпадать", Info, true)
            };
            UpdateValidation(initialHints);
            SubmitButtonText = "Создать ID";
        }
    }
}
