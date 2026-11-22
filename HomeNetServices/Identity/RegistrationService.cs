using HomeNet.Models.Shared.Entity;
using HomeNet.Network.Api;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Validation;
using static HomeNetCore.Interfaces.Services.IRegistrationService;
using static HomeNetCore.Enums.TypeField;
using static HomeNetCore.Enums.ValidationState;

namespace HomeNetServices.Identity
{
    public class RegistrationService(IUserApiClient apiClient) : BaseValidationService(apiClient), IRegistrationService
    {
        protected override ValidationResult InterpretEmailExistence(bool exists) =>
            exists
                ? SetResult(TypeField.EmailType, ValidationState.Error, "Этот Email уже занят")
                : SetResult(TypeField.EmailType, ValidationState.Success, "Email свободен");

        public async Task<Verdict> RegisterUserAsync(UserEntity user)
        {
            List<ValidationResult> validationResults = ValidateInput(user);
            ApplyCachedEmailCheck(user.Email, validationResults);

            if (HasErrors(validationResults))
                return new Verdict(false, validationResults);

            // 1. Назначаем ID пользователю и вычисляем детерминированный SessionId напрямую
            if (user.Id == Guid.Empty)
                user.Id = Guid.NewGuid();

            user.SessionId = ImmortalDeviceProvider.CreateSessionIdForCurrentDevice(user.Id);

            // 2. Отправляем полностью подготовленного юзера на сервер
            IUserApiClient.AuthResponse pushResult = await ApiClient.RegisterUserAsync(user);

            if (pushResult is { IsSuccess: true, User: not null })
            {
                pushResult.User.IsSynced = 1; // Сразу фиксируем, что запись уже на сервере
                return new Verdict(true, validationResults, pushResult.User);
            }

            ValidationResult errorResult = SetResult(EmailType,Error,pushResult.ErrorMessage 
                                                                     ?? "Не удалось зарегистрировать аккаунт.");
            
            return new Verdict(false, [errorResult]);
        }

        private List<ValidationResult> ValidateInput(UserEntity input)
        {
            List<ValidationResult> results = new List<ValidationResult>();

            ValidationResult nameRes = ValidateUserName(input.FirstName);
            ValidationResult emailRes = ValidateEmailFormat(input.Email);
            results.Add(nameRes);
            results.Add(emailRes);

            if (nameRes.State == Error || emailRes.State == Error)
            {
                results.Add(SetResult(PasswordType, Info, "Пароль должен содержать минимум 8 символов, буквы и цифры"));
                results.Add(SetResult(ConfirmedPasswordType, Info, "Пароли должны совпадать"));
                return results;
            }

            results.Add(ValidatePasswordFormat(input.Password));
            results.Add(ValidateConfirmedPassword(input.Password, input.ConfirmPassword));

            return results;
        }
    }
}
