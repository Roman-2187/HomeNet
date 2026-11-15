using HomeNetOrm.Models;

namespace HomeNetOrm.Interfaces.Generators
{
    /// <summary>
    /// ⚙️ ЛОКАЛЬНЫЙ интерфейс генератора для настроек приложения на стороне клиента.
    /// Полностью переведен на Command Object под вызовы через точку! 🦾
    /// </summary>
    internal interface ILocalDeviceSettingsSqlGenerator
    {
        SqlQueryCommand GenerateSelectActiveSession();
        SqlQueryCommand GenerateSelectById();
        SqlQueryCommand GenerateInsertOrUpdate();
    }
}
