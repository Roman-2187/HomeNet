using HomeNet.Models.Shared.Entity;

namespace HomeNetOrm.Interfaces.Repositories
{
    /// <summary>
    /// Серверный репозиторий для управления активными сессиями пользователей в PostgreSQL.
    /// </summary>
    public interface IDeviceSyncRepo
    {
        /// <summary>
        /// Создаёт новую сессию или перезаписывает токен (Id) при повторном входе пользователя.
        /// </summary>
        Task<DeviceSyncEntity> UpsertSessionAsync(Guid sessionId, Guid userId);

        /// <summary>
        /// Ищет активную сессию по её Id (для проверки авторизации в SessionAuthFilter).
        /// </summary>
        Task<DeviceSyncEntity?> GetByIdAsync(Guid sessionId);

        /// <summary>
        /// Аннулирует сессию при выходе пользователя из аккаунта (логауте).
        /// </summary>
        Task DeleteSessionAsync(Guid sessionId);
    }
}
