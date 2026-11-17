using HomeNet.Models.Shared.Entity;
using HomeNet.Network.Api;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Validation;

namespace HomeNetServices.Identity
{
    public class RegistrationService : BaseValidationService, IRegistrationService
    {
        private readonly IDeviceSettingsService _settingsService;

        public RegistrationService(IUserApiClient apiClient, IDeviceSettingsService settingsService) : base(apiClient)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        }

        protected override ValidationResult InterpretEmailExistence(bool exists) =>
            exists
                ? SetResult(TypeField.EmailType, ValidationState.Error, "Этот Email уже занят")
                : SetResult(TypeField.EmailType, ValidationState.Success, "Email свободен");

        public async Task<IRegistrationService.Verdict> RegisterUserAsync(UserEntity user)
        {
            var validationResults = ValidateInput(user);
            ApplyCachedEmailCheck(user.Email, validationResults);

            if (HasErrors(validationResults))
                return new IRegistrationService.Verdict(false, validationResults, null);

            // 1. Назначаем ID пользователю и забираем детерминированный SessionId у SettingsService
            if (user.Id == Guid.Empty)
                user.Id = Guid.NewGuid();

            user.SessionId = _settingsService.CreateSessionIdForUser(user.Id);

            // 2. Отправляем полностью подготовленного юзера на сервер (сервер сохранит ровно этот SessionId!)
            bool success = await ApiClient.PushUserAsync(user);

            if (success)
            {
                user.IsSynced = 1; // Сразу фиксируем, что запись уже на сервере
                return new IRegistrationService.Verdict(true, validationResults, user);
            }

            var errorResult = SetResult(TypeField.EmailType, ValidationState.Error, "Сервер отклонил регистрацию (возможно, Email уже занят).");
            return new IRegistrationService.Verdict(false, [errorResult], null);
        }

        private List<ValidationResult> ValidateInput(UserEntity input)
        {
            var results = new List<ValidationResult>();

            var nameRes = ValidateUserName(input.FirstName);
            var emailRes = ValidateEmailFormat(input.Email);
            results.Add(nameRes);
            results.Add(emailRes);

            if (nameRes.State == ValidationState.Error || emailRes.State == ValidationState.Error)
            {
                results.Add(SetResult(TypeField.PasswordType, ValidationState.Info, "Пароль должен содержать минимум 8 символов, буквы и цифры"));
                results.Add(SetResult(TypeField.ConfirmedPasswordType, ValidationState.Info, "Пароли должны совпадать"));
                return results;
            }

            results.Add(ValidatePasswordFormat(input.Password));
            results.Add(ValidateConfirmedPassword(input.Password, input.ConfirmPassword));

            return results;
        }
    }
}
