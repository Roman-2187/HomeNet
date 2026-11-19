using HomeNetOrm.Models;

namespace HomeNetOrm.Interfaces.Generators
{
    /// <summary>
    /// ⚙️ ЛОКАЛЬНЫЙ интерфейс генератора для настроек приложения на стороне клиента.
    /// Полностью переведен на Command Object под вызовы через точку! 🦾
    /// </summary>
    internal interface ILocalDeviceSettingsSqlGenerator
    {
        /// <summary>
        /// Получает текущую активную сессию на устройстве (IsActive = true).
        /// </summary>
        SqlQueryCommand GenerateSelectActiveSession();

        /// <summary>
        /// Ищет локальную запись сессии по её первичному ключу (SessionId).
        /// </summary>
        SqlQueryCommand GenerateSelectById();

        /// <summary>
        /// Ищет сохранённую локальную сессию и курсоры по Email пользователя.
        /// </summary>
        SqlQueryCommand GenerateSelectByEmail();

        /// <summary>
        /// Сбрасывает флаг активности (IsActive = false) у всех остальных аккаунтов на этом устройстве.
        /// </summary>
        SqlQueryCommand GenerateDeactivateOthers(Guid activeSessionId);

        /// <summary>
        /// Обновляет курсор последней синхронизированной версии профилей для указанного пользователя.
        /// </summary>
        SqlQueryCommand GenerateUpdateLastUserVersion(Guid userId, long newVersion);

        /// <summary>
        /// Обновляет курсор последней синхронизированной версии сообщений для указанного пользователя.
        /// </summary>
        SqlQueryCommand GenerateUpdateLastMessageVersion(Guid userId, long newVersion);

        /// <summary>
        /// Создаёт новую локальную сессию или обновляет существующую при конфликте по UserId.
        /// </summary>
        SqlQueryCommand GenerateInsertOrUpdate();

        /// <summary>
        /// Удаляет локальную сессию и курсоры пользователя при удалении аккаунта (срабатывании надгробия).
        /// </summary>
        SqlQueryCommand GenerateDeleteByUserId(Guid userId);
    }
}
