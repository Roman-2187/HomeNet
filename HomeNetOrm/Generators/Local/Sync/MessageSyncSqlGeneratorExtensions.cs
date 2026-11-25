using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.Generators.Local.Sync
{
    // 🔥 Расширяем публичный интерфейс генератора сообщений
    internal static class MessageSyncSqlGeneratorExtensions
    {
        extension(IMessageSqlGenerator generator)
        {
            public SqlQueryCommand GenerateSelectMessagesForSync(Guid senderId, DateTimeOffset timestamp)
            {
                return generator.CreateQuery()
                    .Emit(Select).Fields().Emit(From).Table()
                    .Emit(Where)
                    .Emit(OpenBracket)
                    .Col(m => m.SenderId).Emit(Equal).Param(m => m.SenderId, "1", senderId)
                    .Emit(Or)
                    .Col(m => m.ReceiverId).Emit(Equal).Param(m => m.ReceiverId, "1", senderId)
                    .Emit(CloseBracket)
                    .Emit(And)
                    .Col(m => m.UpdatedAt).Emit(GreaterThan).Param(m => m.UpdatedAt, timestamp)
                    .Emit(OrderBy).Col(m => m.UpdatedAt).Emit(Asc)
                    .Build();
            }

            internal SqlQueryCommand GenerateSelectUnsynced()
            {
                return generator.CreateQuery()
                    .Emit(Select).Fields().Emit(From).Table()
                    .Emit(Where)
                    .Col(m => m.IsSynced).Emit(Equal).Param(m => m.IsSynced, 0)
                    .Emit(OrderBy).Col(m => m.LocalCreatedAt).Emit(Asc)
                    .Build();
            }

            internal SqlQueryCommand GenerateSelectMessagesSinceVersion(Guid userId, long sinceVersion)
            {
                return generator.CreateQuery()
                    .Emit(Select).Fields().Emit(From).Table()
                    .Emit(Where)
                    .Emit(OpenBracket)
                    .Col(m => m.SenderId).Emit(Equal).Param(m => m.SenderId, "1", userId)
                    .Emit(Or)
                    .Col(m => m.ReceiverId).Emit(Equal).Param(m => m.ReceiverId, "1", userId)
                    .Emit(CloseBracket)
                    .Emit(And)
                    .Col(m => m.Version).Emit(GreaterThan).Param(m => m.Version, sinceVersion)
                    .Emit(OrderBy).Col(m => m.Version).Emit(Asc)
                    .Build();
            }

            internal SqlQueryCommand GenerateUpsert()
            {
                return generator.CreateQuery()
                    .Emit(Insert).Emit(Into).Table()
                    .InsertFields()
                    .Emit(Values)
                    .InsertParameters()
                    .Emit(On).Emit(Conflict)
                    .Emit(OpenBracket).Col(m => m.Id).Emit(CloseBracket)
                    .Emit(Do).Emit(Update).Emit(Set)
                    .Col(m => m.Text).Emit(Equal).Emit(Excluded).Raw(".").Col(m => m.Text).Emit(Comma)
                    .Col(m => m.IsRead).Emit(Equal).Emit(Excluded).Raw(".").Col(m => m.IsRead).Emit(Comma)
                    .Col(m => m.IsDeleted).Emit(Equal).Emit(Excluded).Raw(".").Col(m => m.IsDeleted).Emit(Comma) // 🆕 Добавили IsDeleted
                    .Col(m => m.UpdatedAt).Emit(Equal).Emit(Excluded).Raw(".").Col(m => m.UpdatedAt).Emit(Comma)
                    .Col(m => m.IsSynced).Emit(Equal).Emit(True)
                    .Build();
            }

        }
    }
}
