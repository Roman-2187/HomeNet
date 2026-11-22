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
    /// Стерильная форма авторизации мессенджера SiberNet.
    /// Наследуется от дженерик-базы с указанием конкретного менеджера пользователя.
    /// </summary>
    public partial class AuthenticationViewModel : FormViewModelBase
    {
        private readonly IAuthenticateService _loginService;
       

        [ObservableProperty]
        public partial UserEntity UserData { get; set; } = new();

        public AuthenticationViewModel(
            IAuthenticateService loginService,
            IEventBus eventBus,
            ICastomLogger logger) : base(eventBus,logger)
        {
            _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeInitialHints();
            EventBus.SubscribeAsync<UserSignedIn>(OnUserAuthenticated);
        }

        private async Task OnUserAuthenticated(UserSignedIn msg)
        {
            await Task.Delay(1200);
            ResetForm();
            InitializeInitialHints();
        }

        private void InitializeInitialHints()
        {
            var initialHints = new List<ValidationResult>
            {
                new(TypeField.EmailType, "Введите email", ValidationState.Info, true),
                new(TypeField.PasswordType, "Текущий пароль", ValidationState.Info, true)
            };
            UpdateValidation(initialHints);
            StatusMessage = string.Empty;
        }

        private void ResetForm()
        {
            UserData = new UserEntity();
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();
            SubmitButtonText = "войти";
        }

        #region 🦾 КОМАНДЫ ФОРМЫ АВТОРИЗАЦИИ

        [RelayCommand]
        private async Task CheckEmailAsync()
        {
            var email = UserData.Email;
            if (string.IsNullOrWhiteSpace(email)) return;

            // 1. Сразу показываем промежуточный статус "Проверка..."
            var checkingResult = new ValidationResult
            {
                Field = TypeField.EmailType,
                State = ValidationState.Info,
                Message = "Проверка..."
            };

            var checkingDict = new Dictionary<TypeField, ValidationResult>(ValidationResults)
            {
                [TypeField.EmailType] = checkingResult
            };
            ValidationResults = checkingDict;

            // 2. Проверяем наличие аккаунта через сервис (сработает кэш и переопределение)
            var result = await _loginService.CheckEmailOnBlurAsync(email);

            // 3. Обновляем UI финальным вердиктом ("Пользователь найден" или ошибка)
            var finalDict = new Dictionary<TypeField, ValidationResult>(ValidationResults)
            {
                [TypeField.EmailType] = result
            };
            ValidationResults = finalDict;
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();

            try
            {
                var verdict = await _loginService.CheckUserAsync(UserData);
                ValidationResults = verdict.Messages.ToDictionary(r => r.Field, r => r);

                if (verdict.IsSuccess)
                {
                    StatusMessage = "Вход выполнен успешно";
                    SubmitButtonText = "Входим...";
                    Logger.LogInfo($"[AuthVM] Пользователь {verdict.User?.Email ?? UserData.Email} успешно авторизован.");

                   
                    var finalUser = verdict.User ?? UserData;
                   
                    EventBus.Publish(this, new UserSignedIn(finalUser));
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

        #endregion

        public override void Dispose()
        {
            base.Dispose();
            EventBus.SubscribeAsync<UserSignedIn>(OnUserAuthenticated);
        }
    }
}
