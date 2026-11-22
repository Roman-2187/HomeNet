using HomeNet.Network.Api;
using HomeNetCore.Enums;
using HomeNetCore.Validation;
using static HomeNetCore.Enums.TypeField;
using static HomeNetCore.Enums.ValidationState;

namespace HomeNetServices.Identity
{
    public abstract class BaseValidationService(IUserApiClient apiClient)
    {
        protected readonly IUserApiClient ApiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        
        private string? _lastCheckedEmail;
        private ValidationResult? _lastEmailCheckResult;

        /// <summary>
        /// Наследник сам решает, хорошо или плохо, что Email найден на сервере.
        /// </summary>
        protected abstract ValidationResult InterpretEmailExistence(bool exists);

        public virtual async Task<ValidationResult> CheckEmailOnBlurAsync(string email)
        {
            string cleanEmail = email?.Replace(" ", "").Trim() ?? string.Empty;

            ValidationResult formatCheck = ValidateEmailFormat(cleanEmail);
            if (formatCheck.State == Error)
            {
                ClearEmailCache();
                return formatCheck;
            }

            // Если этот Email уже получил зеленый свет (Success) — отдаем кэш, в API не лезем!
            if (string.Equals(_lastCheckedEmail, cleanEmail, StringComparison.OrdinalIgnoreCase) &&
                _lastEmailCheckResult?.State == Success)
            {
                return _lastEmailCheckResult;
            }

            // Если раньше была ошибка (или это новый Email) — идем в API проверять заново
            try
            {
                bool exists = await ApiClient.CheckEmailExistsAsync(cleanEmail);
                ValidationResult result = InterpretEmailExistence(exists);

                if (result.State == Success) CacheEmailResult(cleanEmail, result);
                else ClearEmailCache();
                
                return result;
            }
            catch (Exception)
            {
                ClearEmailCache();
                return SetResult(EmailType, Error,"Ошибка сети. Не удалось проверить Email.");
            }
        }
        protected void ApplyCachedEmailCheck(string? email, List<ValidationResult> results)
        {
            if (_lastCheckedEmail != email || _lastEmailCheckResult == null) { return; }

            results.RemoveAll(r => r.Field == EmailType);
            results.Add(_lastEmailCheckResult);
        }

      

        protected ValidationResult ValidateUserName(string? userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return SetResult(NameType, Error, "Имя пользователя не может быть пустым");

            return !ValidationFormat.IsValidUserName(userName) 
                ? SetResult(NameType, Error, "Допустимо минимум 3 буквы подряд без пробелов")  
                : SetResult(TypeField.NameType, Success, "Имя пользователя принято");
        }

        protected ValidationResult ValidateEmailFormat
            (string? email, ValidationState successState = Info, string successMsg = "Формат email корректен")
        {
            if (string.IsNullOrWhiteSpace(email))
                return SetResult(EmailType, Error, "Email не может быть пустым");

            return !ValidationFormat.IsValidEmail(email) 
                ? SetResult(EmailType, Error, "Некорректный формат email") 
                : SetResult(EmailType, successState, successMsg);
        }
        
        
        protected ValidationResult ValidatePasswordFormat(string? password, bool checkComplexity = true)
        {
            if (string.IsNullOrWhiteSpace(password))
                return SetResult(PasswordType, Error, "Пароль не может быть пустым");

            // 1. Явно ловим пробел до регулярки
            if (password.Contains(' '))
                return SetResult(PasswordType, Error, "Пароль не должен содержать пробелы");

            // 2. Обновляем текст для регулярки (упоминаем заглавную букву)
            return checkComplexity && !ValidationFormat.IsValidPassword(password)
                ? SetResult(PasswordType, Error, "Минимум 8 символов, 1 заглавная латинская буква и 1 цифра")
                : SetResult(PasswordType, Success, "Пароль принят");
        }

        
        protected ValidationResult ValidateConfirmedPassword(string? password, string? confirmedPassword)
        {
            if (string.IsNullOrWhiteSpace(confirmedPassword))
                return SetResult(ConfirmedPasswordType, Error, "Пароль не может быть пустым");

            return confirmedPassword != password 
                ? SetResult(ConfirmedPasswordType, Error, "Пароли не совпадают") 
                : SetResult(ConfirmedPasswordType, Success, "Пароли совпадают");
        }

        protected bool HasErrors(IEnumerable<ValidationResult> results)
        {
            return results.Any(r => r.State == ValidationState.Error);
        }

        protected ValidationResult SetResult(TypeField field, ValidationState state, string message)
        {
            return new ValidationResult { Field = field, State = state, Message = message };
        }


        private void ClearEmailCache()
        {
            _lastCheckedEmail = null;
            _lastEmailCheckResult = null;
        }
        
        private void CacheEmailResult(string email, ValidationResult result)
        {
            _lastCheckedEmail = email;
            _lastEmailCheckResult = result;
        }

    }
}
