using HomeNet.Network.Api;
using HomeNetCore.Enums;
using HomeNetCore.Validation;

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
            var cleanEmail = email?.Replace(" ", "").Trim() ?? string.Empty;

            var formatCheck = ValidateEmailFormat(cleanEmail);
            if (formatCheck.State == ValidationState.Error)
            {
                ClearEmailCache();
                return formatCheck;
            }

            // Если этот Email уже получил зеленый свет (Success) — отдаем кэш, в API не лезем!
            if (string.Equals(_lastCheckedEmail, cleanEmail, StringComparison.OrdinalIgnoreCase) &&
                _lastEmailCheckResult?.State == ValidationState.Success)
            {
                return _lastEmailCheckResult;
            }

            // Если раньше была ошибка (или это новый Email) — идем в API проверять заново
            try
            {
                bool exists = await ApiClient.CheckEmailExistsAsync(cleanEmail);
                var result = InterpretEmailExistence(exists);

                if (result.State == ValidationState.Success)
                {
                    // Запоминаем ТОЛЬКО успешный результат
                    _lastCheckedEmail = cleanEmail;
                    _lastEmailCheckResult = result;
                }
                else
                {
                    // Ошибку не кэшируем, чтобы при следующем LostFocus проверить снова
                    ClearEmailCache();
                }

                return result;
            }
            catch (Exception)
            {
                ClearEmailCache();
                return SetResult(TypeField.EmailType, ValidationState.Error, "Ошибка сети. Не удалось проверить Email.");
            }
        }
        protected void ApplyCachedEmailCheck(string? email, List<ValidationResult> results)
        {
            if (_lastCheckedEmail == email && _lastEmailCheckResult != null)
            {
                results.RemoveAll(r => r.Field == TypeField.EmailType);
                results.Add(_lastEmailCheckResult);
            }
        }

      

        protected ValidationResult ValidateUserName(string? userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return SetResult(TypeField.NameType, ValidationState.Error, "Имя пользователя не может быть пустым");

            if (!ValidationFormat.IsValidUserName(userName))
                return SetResult(TypeField.NameType, ValidationState.Error, "Допустимо минимум 3 буквы подряд без пробелов");

            return SetResult(TypeField.NameType, ValidationState.Success, "Имя пользователя принято");
        }

        protected ValidationResult ValidateEmailFormat(string? email, ValidationState successState = ValidationState.Info, string successMsg = "Формат email корректен")
        {
            if (string.IsNullOrWhiteSpace(email))
                return SetResult(TypeField.EmailType, ValidationState.Error, "Email не может быть пустым");

            if (!ValidationFormat.IsValidEmail(email))
                return SetResult(TypeField.EmailType, ValidationState.Error, "Некорректный формат email");

            return SetResult(TypeField.EmailType, successState, successMsg);
        }




        protected ValidationResult ValidatePasswordFormat(string? password, bool checkComplexity = true)
        {
            if (string.IsNullOrWhiteSpace(password))
                return SetResult(TypeField.PasswordType, ValidationState.Error, "Пароль не может быть пустым");

            // 1. Явно ловим пробел до регулярки
            if (password.Contains(' '))
                return SetResult(TypeField.PasswordType, ValidationState.Error, "Пароль не должен содержать пробелы");

            // 2. Обновляем текст для регулярки (упоминаем заглавную букву)
            return checkComplexity && !ValidationFormat.IsValidPassword(password)
                ? SetResult(TypeField.PasswordType, ValidationState.Error, "Минимум 8 символов, 1 заглавная латинская буква и 1 цифра")
                : SetResult(TypeField.PasswordType, ValidationState.Success, "Пароль принят");
        }



        protected ValidationResult ValidateConfirmedPassword(string? password, string? confirmedPassword)
        {
            if (string.IsNullOrWhiteSpace(confirmedPassword))
                return SetResult(TypeField.ConfirmedPasswordType, ValidationState.Error, "Пароль не может быть пустым");

            if (confirmedPassword != password)
                return SetResult(TypeField.ConfirmedPasswordType, ValidationState.Error, "Пароли не совпадают");

            return SetResult(TypeField.ConfirmedPasswordType, ValidationState.Success, "Пароли совпадают");
        }

        protected bool HasErrors(IEnumerable<ValidationResult> results)
        {
            return results.Any(r => r.State == ValidationState.Error);
        }

        protected ValidationResult SetResult(TypeField field, ValidationState state, string message)
        {
            return new() { Field = field, State = state, Message = message };
        }


        public void ClearEmailCache()
        {
            _lastCheckedEmail = null;
            _lastEmailCheckResult = null;
        }
    }
}
