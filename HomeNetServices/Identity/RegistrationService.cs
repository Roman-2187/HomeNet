using HomeNet.Models.Shared.Entity;
using HomeNet.Network.Api;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Validation;
using static HomeNetCore.Interfaces.Services.IRegistrationService;

namespace HomeNetServices.Identity
{
    public class RegistrationService : BaseValidationService, IRegistrationService
    {
        public RegistrationService(IUserApiClient apiClient) : base(apiClient)
        {
        }

        protected override ValidationResult InterpretEmailExistence(bool exists) =>
            exists
                ? SetResult(TypeField.EmailType, ValidationState.Error, "Этот Email уже занят")
                : SetResult(TypeField.EmailType, ValidationState.Success, "Email свободен");

        public async Task<Verdict> RegisterUserAsync(UserEntity user)
        {
            var validationResults = ValidateInput(user);
            ApplyCachedEmailCheck(user.Email, validationResults);

            if (HasErrors(validationResults))
                return new Verdict(false, validationResults, null);

            // 1. Назначаем ID пользователю и вычисляем детерминированный SessionId напрямую
            if (user.Id == Guid.Empty)
                user.Id = Guid.NewGuid();

            user.SessionId = ImmortalDeviceProvider.CreateSessionIdForCurrentDevice(user.Id);

            // 2. Отправляем полностью подготовленного юзера на сервер
            var pushResult = await ApiClient.RegisterUserAsync(user);

            if (pushResult.IsSuccess && pushResult.User != null)
            {
                pushResult.User.IsSynced = 1; // Сразу фиксируем, что запись уже на сервере
                return new Verdict(true, validationResults, pushResult.User);
            }

            var errorResult = SetResult(
                TypeField.EmailType,
                ValidationState.Error,
                pushResult.ErrorMessage ?? "Не удалось зарегистрировать аккаунт.");

            return new Verdict(false, [errorResult], null);
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
