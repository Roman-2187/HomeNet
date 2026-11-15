using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Builders;
using HomeNetOrm.Enums;

namespace HomeNetOrm.Schemes
{
    /// <summary>
    /// 📱 ЛОКАЛЬНЫЙ МОДУЛЬ СХЕМ (Для SQLite): Общие таблицы + флаги синхронизации + сессии и курсоры
    /// </summary>
    public class LocalMessengerSchemaModule : BaseMessengerSchemaModule
    {
        public LocalMessengerSchemaModule() : base()
        {
            // Хранилище сессий и курсоров синхронизации для всех аккаунтов на этом устройстве
            var deviceSettings = new TableBuilder<DeviceSettingsEntity>("DeviceSettings");
            deviceSettings.AddColumn(s => s.Id).AsPrimaryKey();
            deviceSettings.AddColumn(s => s.DeviceId).IsRequired();
            deviceSettings.AddColumn(s => s.UserId).IsRequired();
            deviceSettings.AddColumn(s => s.Email);
            deviceSettings.AddColumn(s => s.Password);
            deviceSettings.AddColumn(s => s.IsActive).HasDefault(0, ColumnType.Integer);
            deviceSettings.AddColumn(s => s.LastSyncVersion).HasDefault(0, ColumnType.Integer);
            deviceSettings.AddColumn(s => s.UpdatedAt);

            _schemas.Add(deviceSettings.Generate());
        }

        protected override TableBuilder<UserEntity> BuildUsersSchema()
        {
            var builder = base.BuildUsersSchema();
            builder.AddColumn(u => u.IsSynced).HasDefault(0, ColumnType.Integer);
            builder.AddColumn(f => f.LocalCreatedAt);
            builder.AddColumn(f => f.UpdatedAt);
            return builder;
        }

        protected override TableBuilder<MessageEntity> BuildMessagesSchema()
        {
            var builder = base.BuildMessagesSchema();
            builder.AddColumn(m => m.IsSynced).HasDefault(0, ColumnType.Integer);
            builder.AddColumn(m => m.LocalCreatedAt);
            builder.AddColumn(m => m.UpdatedAt);
            return builder;
        }

        protected override TableBuilder<FriendEntity> BuildFriendsSchema()
        {
            var builder = base.BuildFriendsSchema();
            builder.AddColumn(f => f.IsSynced).HasDefault(0, ColumnType.Integer);
            return builder;
        }
    }
}
