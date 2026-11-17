using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Interfaces.ViewModels.UserZone;
using HomeNetCore.Validation;
using HomeNetPresentation.Services;

namespace HomeNetPresentation.ViewModels
{
    /// <summary>
    /// Стерильная форма авторизации мессенджера SiberNet.
    /// Наследуется от дженерик-базы с указанием конкретного менеджера пользователя.
    /// </summary>
    public partial class AuthenticationViewModel : FormViewModelBase<UserNavigationManager>
    {
        private readonly IAuthenticateService _loginService;
        private readonly ICastomLogger _logger;

        [ObservableProperty] private UserEntity _userData = new();

        public AuthenticationViewModel(
            IAuthenticateService loginService,
            IEventBus eventBus,
            ICastomLogger logger,
            UserNavigationManager navigation) : base(eventBus, navigation)
        {
            _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeInitialHints();
            _eventBus.Subscribe<IUserVm.UserSignedIn>(OnUserAuthenticated);
        }

        private async void OnUserAuthenticated(IUserVm.UserSignedIn msg)
        {
            await Task.Delay(500);
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

        public void ResetForm()
        {
            UserData = new();
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

            var checkingDict = new Dictionary<TypeField, ValidationResult>(ValidationResults);
            checkingDict[TypeField.EmailType] = checkingResult;
            ValidationResults = checkingDict;

            // 2. Проверяем наличие аккаунта через сервис (сработает кэш и переопределение)
            var result = await _loginService.CheckEmailOnBlurAsync(email);

            // 3. Обновляем UI финальным вердиктом ("Пользователь найден" или ошибка)
            var finalDict = new Dictionary<TypeField, ValidationResult>(ValidationResults);
            finalDict[TypeField.EmailType] = result;
            ValidationResults = finalDict;
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            StatusMessage = string.Empty;
            ValidationResults = new Dictionary<TypeField, ValidationResult>();

            try
            {
                IAuthenticateService.Verdict verdict = await _loginService.CheckUserAsync(UserData);
                ValidationResults = verdict.Messages.ToDictionary(r => r.Field, r => r);

                if (verdict.IsSuccess)
                {
                    StatusMessage = "Вход выполнен успешно";
                    SubmitButtonText = "Входим...";
                    _logger.LogInfo($"[AuthVM] Пользователь {verdict.User?.Email ?? UserData.Email} успешно авторизован.");

                    var finalUser = verdict.User ?? UserData;
                    _eventBus.Publish(this, new IUserVm.UserSignedIn(finalUser));
                }
                else
                {
                    StatusMessage = "Есть ошибки в полях";
                    _logger.LogWarning($"[AuthVM] Неудачная попытка входа для: {UserData.Email}");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"При входе произошла ошибка: {ex.Message}";
                _logger.LogError($"[AuthVM] Критический сбой авторизации: {ex.Message}");
            }
        }

        [RelayCommand]
        private void OpenRegisterZone()
        {
            _logger.LogInfo("[AuthVM] Запрос перехода на экран регистрации через EventBus.");
            _eventBus.Publish(this, new IUserVm.OpenRegistration());
        }

        [RelayCommand]
        private void Cancel()
        {
            ResetForm();
            InitializeInitialHints();
            _eventBus.Publish(this, new IUserVm.BackToStart());
        }

        #endregion

        public override void Dispose()
        {
            base.Dispose();
            _eventBus.Unsubscribe<IUserVm.UserSignedIn>(OnUserAuthenticated);
        }
    }
}
