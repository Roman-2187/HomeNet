using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNet.Network.Api;
using HomeNetCore.Enums;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Validation;
using static HomeNetCore.Interfaces.Services.IDeleteService;
using static HomeNetCore.Enums.ValidationState;

namespace HomeNetServices.Identity
{
    public class DeleteService(IUserApiClient apiClient, ICastomLogger logger) : IDeleteService
    {
        private readonly IUserApiClient _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        private readonly ICastomLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task<IEnumerable<UserEntity>> GetAllUsersAsync()
        {
            // 🔥 Выгружаем актуальный список всех юзеров с сервера (API)
            IReadOnlyCollection<UserEntity>? users = await _apiClient.GetAllUsersAsync();
            return users;
        }


        public async Task<SearchVerdict> SearchUserAsync(string targetUserId)
        {
            List<ValidationResult> validationResults = ValidateIdInput(targetUserId);

            if (validationResults.Any(r => r.State == Error))
                return new SearchVerdict(false, validationResults, null);

            Guid id = Guid.Parse(targetUserId);
            try
            {
                // 🔥 Ищем на сервере (API)
                UserEntity? user = await _apiClient.GetUserByIdAsync(id);
                ValidationResult res = validationResults.First();

                if (user != null)
                {
                    return new SearchVerdict(true, validationResults, user)
                        .WithResult(res, Success, $"Найден: {user.FirstName} {user.LastName} ({user.Email})");
                }

                return new SearchVerdict(false, validationResults, null)
                    .WithResult(res, Error, $"Пользователь с ID {id} не существует на сервере.");
            }
            catch (Exception ex)
            {
                ValidationResult errorResult = new() { State = Error, Message = $"Ошибка сети: {ex.Message}" };
                return new SearchVerdict(false, [errorResult], null);
            }
        }

        public async Task<DeleteVerdict> DeleteUserAsync(string targetUserId, UserEntity? selectedUser)
        {
            Guid id = selectedUser?.Id ?? (Guid.TryParse(targetUserId, out Guid parsedId) ? parsedId : Guid.Empty);

            List<ValidationResult> validationResults = [];
            ValidationResult idRes = new() { Field = TypeField.IdType, State = Success };
            validationResults.Add(idRes);

            if (id == Guid.Empty)
            {
                idRes.Update(Error, "Ошибка: Некорректный ID пользователя!");
                return new DeleteVerdict(false, validationResults, id);
            }

            try
            {
                string userInfo = selectedUser != null
                    ? $"{selectedUser.FirstName} {selectedUser.LastName} (ID {id})"
                    : $"ID {id} (Точечно)";

                // 🔥 Удаляем на сервере (API)
                bool success = await _apiClient.DeleteUserAsync(id);

                if (!success)
                {
                    idRes.Update(Error, "Сервер недоступен или удаление отклонено.");
                    return new DeleteVerdict(false, validationResults, id);
                }

                // 🔥 После удаления запрашиваем актуальный список с сервера
                IReadOnlyCollection<UserEntity> allUsers = await _apiClient.GetAllUsersAsync();
                _logger.LogDebug($"В системе {allUsers.Count} пользователей (после удаления)");

                idRes.Update(Success, $"Пользователь с ID {id} успешно удален.");

                string readyHistoryLine = $"[{DateTime.Now:HH:mm:ss}] ❌ Удален: {userInfo}";

                DeleteVerdict verdict = new(true, validationResults, id, allUsers)
                {
                    HistoryMessage = readyHistoryLine
                };

                return verdict;
            }
            catch (Exception ex)
            {
                idRes.Update(Error, $"Ошибка удаления: {ex.Message}");
                return new DeleteVerdict(false, validationResults, id);
            }
        }

        private List<ValidationResult> ValidateIdInput(string targetUserId)
        {
            ValidationResult res = new ValidationResult { Field = TypeField.IdType };

            if (string.IsNullOrWhiteSpace(targetUserId))
                return [res.Update(Error, "Ошибка: ID не может быть пустым!")];

            if (!Guid.TryParse(targetUserId, out _))
                return [res.Update(Error, "Ошибка: ID должен быть валидным Guid!")];

            return [res.Update(Success, "ID корректен для поиска")];
        }
    }
}