using HomeNet.Models.Shared.Entity;

namespace HomeNetOrm.Interfaces.Repositories
{
    /// <summary>
    /// Локальный репозиторий для хранения активных сессий и курсоров синхронизации на устройстве.
    /// </summary>
    public interface IDeviceSettingsRepo
    {
        /// <summary>
        /// Возвращает текущую активную сессию на устройстве (для автологина и запросов синхронизации).
        /// </summary>
        Task<SettingsEntity?> GetActiveSessionAsync();

        /// <summary>
        /// Ищет локальную запись сессии по её уникальному идентификатору (SessionId).
        /// </summary>
        Task<SettingsEntity?> GetBySessionIdAsync(Guid sessionId);

        /// <summary>
        /// Ищет сохранённую локальную сессию и курсоры синхронизации по Email пользователя.
        /// </summary>
        Task<SettingsEntity?> GetByEmailAsync(string email);

        /// <summary>
        /// Сохраняет сессию пользователя, сохраняя накопленные ранее курсоры версий и деактивируя остальные аккаунты на ПК.
        /// </summary>
        Task SaveSettingsAsync(SettingsEntity settings);

        /// <summary>
        /// Сдвигает курсор последней синхронизированной версии профилей для указанного пользователя.
        /// </summary>
        Task UpdateLastUserVersionAsync(Guid userId, long newVersion);

        /// <summary>
        /// Сдвигает курсор последней синхронизированной версии сообщений для указанного пользователя.
        /// </summary>
        Task UpdateLastMessageVersionAsync(Guid userId, long newVersion);

        /// <summary>
        /// Полностью удаляет локальную сессию и курсоры пользователя (используется при удалении аккаунта).
        /// </summary>
        Task DeleteByUserIdAsync(Guid userId);
    }
}
