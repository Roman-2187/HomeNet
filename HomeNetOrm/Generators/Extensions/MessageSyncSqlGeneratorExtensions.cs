using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.Generators.Extensions
{
    // 🔥 Расширяем публичный интерфейс генератора сообщений
    internal static class MessageSyncSqlGeneratorExtensions
    {
        public static SqlQueryCommand GenerateSelectMessagesForSync(this IMessageSqlGenerator generator, Guid senderId, DateTimeOffset timestamp)
        {
            return generator.CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where)
                .Emit(OpType.OpenBracket)
                    .Col(m => m.SenderId).Emit(OpType.Equal).Param(m => m.SenderId, "1", senderId)
                    .Emit(OpType.Or)
                    .Col(m => m.ReceiverId).Emit(OpType.Equal).Param(m => m.ReceiverId, "1", senderId)
                .Emit(OpType.CloseBracket)
                .Emit(OpType.And)
                    .Col(m => m.UpdatedAt).Emit(OpType.GreaterThan).Param(m => m.UpdatedAt, timestamp)
                .Emit(PhraseType.OrderBy).Col(m => m.UpdatedAt).Emit(OpType.Asc)
                .Build();
        }


        // 📥 МЕТОД 3: Выборка сообщений пользователя по глобальной версии (вместо метки 00:00:00)
        internal static SqlQueryCommand GenerateSelectMessagesSinceVersion(this IMessageSqlGenerator generator, Guid userId, long sinceVersion)
        {
            return generator.CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where)
                .Emit(OpType.OpenBracket)
                    .Col(m => m.SenderId).Emit(OpType.Equal).Param(m => m.SenderId, "1", userId)
                    .Emit(OpType.Or)
                    .Col(m => m.ReceiverId).Emit(OpType.Equal).Param(m => m.ReceiverId, "1", userId)
                .Emit(OpType.CloseBracket)
                .Emit(OpType.And)
                    .Col(m => m.Version).Emit(OpType.GreaterThan).Param(m => m.Version, sinceVersion)
                .Emit(PhraseType.OrderBy).Col(m => m.Version).Emit(OpType.Asc)
                .Build();
        }


        internal static SqlQueryCommand GenerateUpsert(this IMessageSqlGenerator generator)
        {
            return generator.CreateQuery()
                .Emit(CmdType.Insert).Emit(PhraseType.Into).Table()
                .InsertFields()
                .Emit(PhraseType.Values)
                .InsertParameters()
                .Emit(PhraseType.On).Emit(PhraseType.Conflict)
                    .Emit(OpType.OpenBracket).Col(m => m.Id).Emit(OpType.CloseBracket)
                .Emit(PhraseType.Do).Emit(CmdType.Update).Emit(PhraseType.Set)
                    .Col(m => m.Text).Emit(OpType.Equal).Raw("EXCLUDED.\"text\"").Emit(OpType.Comma)
                    .Col(m => m.IsRead).Emit(OpType.Equal).Raw("EXCLUDED.\"is_read\"").Emit(OpType.Comma)
                    .Col(m => m.UpdatedAt).Emit(OpType.Equal).Raw("EXCLUDED.\"updated_at\"").Emit(OpType.Comma)
                    .Col(m => m.IsSynced).Emit(OpType.Equal).Raw("1")
                .Build();
        }

    }
}
