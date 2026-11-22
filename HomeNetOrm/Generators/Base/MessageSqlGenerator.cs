using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.Generators.Base
{
    internal class MessageSqlGenerator(
        IEnumerable<TableSchema> schemas,
        ISqlDialectTranslator translator,
        ICastomLogger logger)
        : BaseSqlGenerator<MessageEntity>(schemas, translator, logger), IMessageSqlGenerator
    {
        public SqlQueryCommand GenerateSelectChatHistory(Guid senderId, Guid receiverId) =>
        CreateQuery()
            .Emit(Select).Fields().Emit(From).Table()
            .Emit(Where)
            .Emit(OpenBracket)
                // Условие 1: Я отправил другу (SenderId1, ReceiverId1)
                .Col(m => m.SenderId).Emit(Equal).Param(m => m.SenderId, "1", senderId)
                .Emit(And)
                .Col(m => m.ReceiverId).Emit(Equal).Param(m => m.ReceiverId, "1", receiverId)
            .Emit(CloseBracket)
            .Emit(Or)
            .Emit(OpenBracket)
                // Условие 2: Друг отправил мне (SenderId2, ReceiverId2)
                .Col(m => m.SenderId).Emit(Equal).Param(m => m.SenderId, "2", receiverId)
                .Emit(And)
                .Col(m => m.ReceiverId).Emit(Equal).Param(m => m.ReceiverId, "2", senderId)
            .Emit(CloseBracket)
            .Emit(OrderBy).Col(m => m.LocalCreatedAt).Emit(Asc)
            .Build();

        public SqlQueryCommand GenerateMarkAsRead(Guid senderId, Guid receiverId) =>
            CreateQuery()
                .Emit(Update).Table()
                .Emit(Set).Col(m => m.IsRead).Emit(Equal).Emit(True)
                .Emit(Where)
                    .Col(m => m.SenderId).Emit(Equal).Param(m => m.SenderId, senderId)
                    .Emit(And)
                    .Col(m => m.ReceiverId).Emit(Equal).Param(m => m.ReceiverId, receiverId)
                    .Emit(And)
                    .Col(m => m.IsRead).Emit(Equal).Emit(False)
                .Build();

        public SqlQueryCommand GenerateUnreadCount(Guid currentUserId, Guid friendId) =>
            CreateQuery()
                .Emit(Select).CountAll().Emit(From).Table()
                .Emit(Where)
                    .Col(m => m.ReceiverId).Emit(Equal).Param(m => m.ReceiverId, currentUserId)
                    .Emit(And)
                    .Col(m => m.SenderId).Emit(Equal).Param(m => m.SenderId, friendId)
                    .Emit(And)
                    .Col(m => m.IsRead).Emit(Equal).Emit(False)
                .Build();

        public SqlQueryCommand GenerateClearChatHistory(Guid senderId, Guid receiverId) =>
            CreateQuery()
                .Emit(Delete).Emit(From).Table()
                .Emit(Where)
                .Emit(OpenBracket)
                    .Col(m => m.SenderId).Emit(Equal).Param(m => m.SenderId, "1", senderId)
                    .Emit(And)
                    .Col(m => m.ReceiverId).Emit(Equal).Param(m => m.ReceiverId, "1", receiverId)
                .Emit(CloseBracket)
                .Emit(Or)
                .Emit(OpenBracket)
                    .Col(m => m.SenderId).Emit(Equal).Param(m => m.SenderId, "2", receiverId)
                    .Emit(And)
                    .Col(m => m.ReceiverId).Emit(Equal).Param(m => m.ReceiverId, "2", senderId)
                .Emit(CloseBracket)
                .Build();
        
        
        
        public SqlQueryCommand GenerateSoftDelete(Guid messageId) =>
            CreateQuery()
                .Emit(Update).Table()
                .Emit(Set)
                .Col(m => m.Text).Emit(Equal).Emit(Null) // Затираем текст в NULL
                .Emit(Comma)
                .Col(m => m.IsDeleted).Emit(Equal).Emit(True) // Ставим флаг удаления
                .Emit(Comma)
                .Col(m => m.UpdatedAt).Emit(Equal).Param(m => m.UpdatedAt, DateTimeOffset.UtcNow)
                .Emit(Where)
                .Col(m => m.Id).Emit(Equal).Param(m => m.Id, messageId)
                .Build();
    }
    
    
}
