using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;

namespace HomeNetOrm.Generators.Base
{
    // 🔥 Класс чист от репликации, занимается только доменной логикой пользователей
    internal class UserSqlGenerator(
        IEnumerable<TableSchema> schemas,
        ISqlDialectTranslator translator,
        ICastomLogger logger)
        : BaseSqlGenerator<UserEntity>(schemas, translator, logger), IUserSqlGenerator
    {
        public SqlQueryCommand GenerateSelectByEmail(string email) =>
            CreateQuery()
                .Emit(Select).Fields().Emit(From).Table()
                .Emit(Where).Col(u => u.Email).Emit(Equal).Param(u => u.Email, email)
                .Build();

        public SqlQueryCommand GenerateEmailExists(string email) =>
            CreateQuery()
                .Emit(Select).CountAll().Emit(From).Table()
                .Emit(Where).Col(u => u.Email).Emit(Equal).Param(u => u.Email, email)
                .Build();
    }
}
