using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNetCore.Interfaces.ViewModels.UserZone
{
    public interface IDeviceSettingsVM
    {

        /// <summary>
        /// Событие: активная сессия пользователя успешно записана в SQLite (device_settings) и готова для синхронизации.
        /// </summary>
        public record SessionPersisted(Guid DeviceId, Guid UserId);
    }
}
