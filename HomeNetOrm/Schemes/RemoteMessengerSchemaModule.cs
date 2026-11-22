using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Builders;

namespace HomeNetOrm.Schemes
{
    /// <summary>
    /// 🌐 УДАЛЕННЫЙ МОДУЛЬ СХЕМ (Для PostgresSQL): Общие таблицы с триггерами версий + курсор девайсов + версионируемая таблица надгробий
    /// </summary>
    public class RemoteMessengerSchemaModule : BaseMessengerSchemaModule
    {
        public RemoteMessengerSchemaModule()
        {
           




            var deviceSync = new TableBuilder<DeviceSyncEntity>("DeviceSync");

            deviceSync.AddColumn(d => d.Id).AsPrimaryKey(); // Актуальный SessionId
            deviceSync.AddColumn(d => d.UserId).IsRequired(); // Гарантирует 1 актуальную сессию на пользователя
            
            deviceSync.AddColumn(d => d.CreatedAt);
            deviceSync.AddColumn(d => d.UpdatedAt);

            Schemas.Add(deviceSync.Generate());


            // 2. Серверная таблица надгробий (Tombstones) с отслеживанием версий
            var deletedRecords = new TableBuilder<DeletedRecordEntity>("DeletedRecords")
                .WithVersionTracking();

            deletedRecords.AddColumn(d => d.Id).AsPrimaryKey();
            deletedRecords.AddColumn(d => d.EntityType).HasLength(50).IsRequired();
            deletedRecords.AddColumn(d => d.DeletedAt).IsRequired();
            deletedRecords.AddColumn(d => d.Version);

            Schemas.Add(deletedRecords.Generate());
        }

        protected override TableBuilder<UserEntity> BuildUsersSchema()
        {
            var builder = base.BuildUsersSchema().WithVersionTracking();
            builder.AddColumn(u => u.Password).HasLength(100).IsRequired();
            builder.AddColumn(f => f.ServerCreatedAt);
            builder.AddColumn(f => f.UpdatedAt);
            builder.AddColumn(u => u.Version);
            return builder;
        }

        protected override TableBuilder<MessageEntity> BuildMessagesSchema()
        {
            var builder = base.BuildMessagesSchema().WithVersionTracking();

            builder.AddColumn(m => m.ServerCreatedAt);
            builder.AddColumn(m => m.UpdatedAt);
            builder.AddColumn(m => m.Version);
            return builder;
        }

        protected override TableBuilder<FriendEntity> BuildFriendsSchema()
        {
            var builder = base.BuildFriendsSchema().WithVersionTracking();
            builder.AddColumn(f => f.Version);
            return builder;
        }
    }
}
