using HomeNet.Models.Shared.Entity;
using HomeNet.Network.Api;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Validation;

namespace HomeNetServices.Identity
{
    public class AuthenticateService : BaseValidationService, IAuthenticateService
    {
        public AuthenticateService(IUserApiClient apiClient) : base(apiClient) { }

        protected override ValidationResult InterpretEmailExistence(bool exists) =>
            exists
                ? SetResult(TypeField.EmailType, ValidationState.Success, "Пользователь найден")
                : SetResult(TypeField.EmailType, ValidationState.Error, "Аккаунт с таким Email не найден");

        public async Task<IAuthenticateService.Verdict> CheckUserAsync(UserEntity userInput)
        {
            ArgumentNullException.ThrowIfNull(userInput);

            if (!string.IsNullOrWhiteSpace(userInput.Email))
                userInput.Email = userInput.Email.Replace(" ", "");

            var validationResults = new List<ValidationResult>
            {
                ValidateEmailFormat(userInput.Email, ValidationState.Success, "Email принят"),
                ValidatePasswordFormat(userInput.Password, checkComplexity: false)
            };

            // Подмешиваем результат проверки существования Email на потерю фокуса
            ApplyCachedEmailCheck(userInput.Email, validationResults);

            if (HasErrors(validationResults))
                return new IAuthenticateService.Verdict(false, validationResults, null);

            var authenticatedUser = await ApiClient.AuthenticateAsync(userInput.Email!, userInput.Password!);

            if (authenticatedUser != null)
                return new IAuthenticateService.Verdict(true, validationResults, authenticatedUser);

            var errorResult = SetResult(TypeField.PasswordType, ValidationState.Error, "Неверный пароль (или сервер недоступен).");
            return new IAuthenticateService.Verdict(false, [errorResult], null);
        }
    }
}
