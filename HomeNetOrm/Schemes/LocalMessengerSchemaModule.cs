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
        public LocalMessengerSchemaModule()
        {
            TableBuilder<SettingsEntity> clientSettings = new("Settings");
            
            clientSettings.AddColumn(s => s.Id).AsPrimaryKey(); // Единый SessionId
          
            clientSettings.AddColumn(s => s.UserId).IsRequired().IsUnique();
            clientSettings.AddColumn(s => s.Email);
            clientSettings.AddColumn(s => s.IsActive).HasDefault(0, ColumnType.Integer);
            clientSettings.AddColumn(s => s.UpdatedAt);
            clientSettings.AddColumn(d => d.LastUserVersion);
            clientSettings.AddColumn(s => s.LastMessageVersion);
            Schemas.Add(clientSettings.Generate());


        }

        protected override TableBuilder<UserEntity> BuildUsersSchema()
        {
            TableBuilder<UserEntity> builder = base.BuildUsersSchema();
            builder.AddColumn(u => u.IsSynced).HasDefault(0, ColumnType.Integer);
            builder.AddColumn(f => f.LocalCreatedAt);
            builder.AddColumn(f => f.UpdatedAt);
            return builder;
        }

        protected override TableBuilder<MessageEntity> BuildMessagesSchema()
        {
            TableBuilder<MessageEntity> builder = base.BuildMessagesSchema();
            builder.AddColumn(m => m.IsSynced).HasDefault(0, ColumnType.Integer);
            builder.AddColumn(m => m.LocalCreatedAt);
            builder.AddColumn(m => m.UpdatedAt);
            return builder;
        }

        protected override TableBuilder<FriendEntity> BuildFriendsSchema()
        {
            TableBuilder<FriendEntity> builder = base.BuildFriendsSchema();
            builder.AddColumn(f => f.IsSynced).HasDefault(0, ColumnType.Integer);
            return builder;
        }
    }
}
