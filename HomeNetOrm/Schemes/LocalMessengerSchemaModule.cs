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
            var clientSettings = new TableBuilder<SettingsEntity>("Settings");
            
            clientSettings.AddColumn(s => s.Id).AsPrimaryKey(); // Единый SessionId
          
            clientSettings.AddColumn(s => s.UserId).IsRequired().IsUnique();
            clientSettings.AddColumn(s => s.Email);
            clientSettings.AddColumn(s => s.IsActive).HasDefault(0, ColumnType.Integer);
            clientSettings.AddColumn(s => s.UpdatedAt);

            _schemas.Add(clientSettings.Generate());


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
