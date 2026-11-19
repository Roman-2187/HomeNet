using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
using static HomeNetOrm.Enums.Sql;

namespace HomeNetOrm.Generators.Extensions
{
    // 🔥 Расширяем публичный интерфейс генератора пользователей
    public static class UserSyncSqlGeneratorExtensions
    {
        // 📥 МЕТОД 1: Выборка лога изменений пользователей для синхронизации
        internal static SqlQueryCommand GenerateSelectUsersForSync(this IUserSqlGenerator generator, DateTimeOffset timestamp)
        {
            return generator.CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where)
                    // Выгребаем измененных или добавленных пользователей по единому таймстампу
                    .Col(u => u.UpdatedAt).Emit(OpType.GreaterThan).Param(u => u.UpdatedAt, timestamp)
                .Emit(PhraseType.OrderBy).Col(u => u.UpdatedAt).Emit(OpType.Asc)
                .Build();
        }



        // 📥 МЕТОД 3: Выборка пользователей по глобальной версии (вместо вычитки всей таблицы)
        internal static SqlQueryCommand GenerateSelectUsersSinceVersion(this IUserSqlGenerator generator, long sinceVersion)
        {
            return generator.CreateQuery()
                .Emit(CmdType.Select).Fields().Emit(PhraseType.From).Table()
                .Emit(PhraseType.Where)
                    .Col(u => u.Version).Emit(OpType.GreaterThan).Param(u => u.Version, sinceVersion)
                .Emit(PhraseType.OrderBy).Col(u => u.Version).Emit(OpType.Asc)
                .Build();
        }


        // 💾 МЕТОД 2: Атомарный UPSERT входящих пакетов пользователей
        internal static SqlQueryCommand GenerateUserUpsert(this IUserSqlGenerator generator)
        {
            return generator.CreateQuery()
                .Emit(CmdType.Insert).Emit(PhraseType.Into).Table()
                .InsertFields()
                .Emit(PhraseType.Values)
                .InsertParameters()
                .Emit(PhraseType.On).Emit(PhraseType.Conflict)
                    .Emit(OpType.OpenBracket).Col(u => u.Email).Emit(OpType.CloseBracket)
                .Emit(PhraseType.Do).Emit(CmdType.Update).Emit(PhraseType.Set)
                    .Col(u => u.Id).Emit(OpType.Equal).Raw("EXCLUDED.\"id\"").Emit(OpType.Comma)
                    .Col(u => u.FirstName).Emit(OpType.Equal).Raw("EXCLUDED.\"first_name\"").Emit(OpType.Comma)
                    .Col(u => u.LastName).Emit(OpType.Equal).Raw("EXCLUDED.\"last_name\"").Emit(OpType.Comma)
                    .Col(u => u.PhoneNumber).Emit(OpType.Equal).Raw("EXCLUDED.\"phone_number\"").Emit(OpType.Comma)
                    .Col(u => u.IsSynced).Emit(OpType.Equal).Raw("1")
                .Build();
        }

    }
}
