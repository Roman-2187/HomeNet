using HomeNet.Models.Shared.Entity;
using HomeNet.Network.Api;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Validation;
using static HomeNetCore.Enums.TypeField;
using static HomeNetCore.Enums.ValidationState;
namespace HomeNetServices.Identity
{
    public class AuthenticateService(IUserApiClient apiClient) : BaseValidationService(apiClient), IAuthenticateService
    {
        protected override ValidationResult InterpretEmailExistence(bool exists) =>
            exists
                ? SetResult(EmailType, Success, "Пользователь найден")
                : SetResult(EmailType, Error, "Аккаунт с таким Email не найден");

        public async Task<IAuthenticateService.Verdict> CheckUserAsync(UserEntity userInput)
        {
            ArgumentNullException.ThrowIfNull(userInput);

            if (!string.IsNullOrWhiteSpace(userInput.Email))
                userInput.Email = userInput.Email.Replace(" ", "");

            List<ValidationResult> validationResults =
            [
                ValidateEmailFormat(userInput.Email, ValidationState.Success, "Email принят"),
                ValidatePasswordFormat(userInput.Password, checkComplexity: false)
            ];

            ApplyCachedEmailCheck(userInput.Email, validationResults);

            if (HasErrors(validationResults))
                return new IAuthenticateService.Verdict(false, validationResults, null);

            // Всегда генерируем свежий токен сессии на базе текущего устройства
            var sessionId = ImmortalDeviceProvider.CreateSessionIdForCurrentDevice(Guid.NewGuid());

            var authResult = await ApiClient.AuthenticateAsync(userInput.Email!, userInput.Password!, sessionId);

            if (authResult is { IsSuccess: true, User: not null })
                return new IAuthenticateService.Verdict(true, validationResults, authResult.User);

            var errorResult = SetResult(
                PasswordType,
                Error,
                authResult.ErrorMessage ?? "Не удалось выполнить вход.");

            return new IAuthenticateService.Verdict(false, [errorResult], null);
        }

    }
} 
