using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNet.Models.Shared.Entity
{
    public class DeletedRecordEntity
    {
        public Guid Id { get; set; } // GUID удалённой записи (Primary Key)
        public string EntityType { get; set; } = string.Empty; // "User", "Message", "Friend"
        public DateTimeOffset DeletedAt { get; set; } // Серверное время удаления
        public long Version { get; set; } // Версия события удаления для инкрементальной синхронизации
    }
}
