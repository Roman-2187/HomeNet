using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Builders;
using HomeNetOrm.Models;

namespace HomeNetOrm.Schemes
{
    /// <summary>
    /// 🏗️ БАЗОВЫЙ МОДУЛЬ СХЕМ: Содержит только общие поля для обеих СУБД
    /// </summary>
    public abstract class BaseMessengerSchemaModule
    {
        protected readonly List<TableSchema> Schemas = [];

        protected BaseMessengerSchemaModule()
        {
            BuildCommonSchemas();
        }

        private void BuildCommonSchemas()
        {
            // 1. СХЕМА ТАБЛИЦЫ ПОЛЬЗОВАТЕЛЕЙ (Users)
            var users = BuildUsersSchema();
            Schemas.Add(users.Generate());

            // 2. СХЕМА ТАБЛИЦЫ СООБЩЕНИЙ (Messages)
            var messages = BuildMessagesSchema();
            Schemas.Add(messages.Generate());

            // 3. СХЕМА ТАБЛИЦЫ КОНТАКТОВ / ДРУЗЕЙ (Friends)
            var friends = BuildFriendsSchema();
            Schemas.Add(friends.Generate());
        }

        protected virtual TableBuilder<UserEntity> BuildUsersSchema()
        {
            var builder = new TableBuilder<UserEntity>("Users");
            builder.AddColumn(u => u.Id).AsPrimaryKey();
           
            builder.AddColumn(u => u.FirstName).HasLength(50).IsRequired();
            builder.AddColumn(u => u.LastName).HasLength(50);
            builder.AddColumn(u => u.PhoneNumber).HasLength(50);
            builder.AddColumn(u => u.Email).HasLength(50).IsRequired().IsUnique();
            

          
            return builder;
        }

        protected virtual TableBuilder<MessageEntity> BuildMessagesSchema()
        {
            var builder = new TableBuilder<MessageEntity>("Messages");
            builder.AddColumn(m => m.Id).AsPrimaryKey();
            builder.AddColumn(m => m.SenderId).IsRequired();
            builder.AddColumn(m => m.ReceiverId).IsRequired();
            builder.AddColumn(m => m.Text).AsText().IsNullable();
            builder.AddColumn(m => m.MediaType).AsText();
            builder.AddColumn(m => m.IsRead);
            builder.AddColumn(m => m.IsDeleted); // <-- Сразу в обе базы

            // 🔥 НАШ НОВЫЙ СТАНДАРТ РАСПРЕДЕЛЕННЫХ ТАЙМСТАМПОВ
            
            return builder;
        }

        protected virtual TableBuilder<FriendEntity> BuildFriendsSchema()
        {
            var builder = new TableBuilder<FriendEntity>("Friends");
            builder.AddColumn(f => f.Id).AsPrimaryKey();
            builder.AddColumn(f => f.UserId).IsRequired();
            builder.AddColumn(f => f.FriendId).IsRequired();

            // 🔥 НАШ НОВЫЙ СТАНДАРТ РАСПРЕДЕЛЕННЫХ ТАЙМСТАМПОВ
           
            return builder;
        }

        public IEnumerable<TableSchema> GetSchemas() => Schemas;
    }
}
