using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.Generators.Local.Sync
{
    // 🔥 Расширяем публичный интерфейс генератора пользователей
    public static class UserSyncSqlGeneratorExtensions
    {
       

        // 📥 МЕТОД 3: Выборка пользователей по глобальной версии (вместо вычитки всей таблицы)
        internal static SqlQueryCommand GenerateSelectUsersSinceVersion(this IUserSqlGenerator generator, long sinceVersion)
        {
            return generator.CreateQuery()
                .Emit(Select).Fields().Emit(From).Table()
                .Emit(Where)
                    .Col(u => u.Version).Emit(GreaterThan).Param(u => u.Version, sinceVersion)
                .Emit(OrderBy).Col(u => u.Version).Emit(Asc)
                .Build();
        }


        // 💾 МЕТОД 2: Атомарный UPSERT входящих пакетов пользователей
        internal static SqlQueryCommand GenerateUserUpsert(this IUserSqlGenerator generator)
        {
            return generator.CreateQuery()
                .Emit(Insert).Emit(Into).Table()
                .InsertFields()
                .Emit(Values)
                .InsertParameters()
                .Emit(On).Emit(Conflict)
                    .Emit(OpenBracket).Col(u => u.Email).Emit(CloseBracket)
                .Emit(Do).Emit(Update).Emit(Set)
                    .Col(u => u.Id).Emit(Equal).Emit(Excluded).Raw(".").Col(u => u.Id).Emit(Comma)
                    .Col(u => u.FirstName).Emit(Equal).Emit(Excluded).Raw(".").Col(u => u.FirstName).Emit(Comma)
                    .Col(u => u.LastName).Emit(Equal).Emit(Excluded).Raw(".").Col(u => u.LastName).Emit(Comma)
                    .Col(u => u.PhoneNumber).Emit(Equal).Emit(Excluded).Raw(".").Col(u => u.PhoneNumber).Emit(Comma)
                    .Col(u => u.IsSynced).Emit(Equal).Emit(True)
                .Build();
        }


    }
}
