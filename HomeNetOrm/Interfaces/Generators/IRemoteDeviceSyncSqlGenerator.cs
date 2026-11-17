using System;
using HomeNetOrm.Models;

namespace HomeNetOrm.Interfaces.Generators
{

    namespace HomeNetOrm.Interfaces.Generators
    {
        /// <summary>
        /// Интерфейс генератора сессий и курсоров синхронизации устройств на сервере.
        /// Полностью переведен на Command Object.
        /// </summary>
        internal interface IRemoteDeviceSyncSqlGenerator
        {
            SqlQueryCommand GenerateSessionUpsert();
            SqlQueryCommand GenerateSelectById(Guid sessionId);
            SqlQueryCommand GenerateUpdateLastVersion(Guid sessionId, long newVersion);
            SqlQueryCommand GenerateDeleteById(Guid sessionId);
        }
    }
}
