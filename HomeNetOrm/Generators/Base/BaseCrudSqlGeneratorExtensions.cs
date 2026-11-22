using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Builders;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Models;
// Где лежит твоя BaseEntity

namespace HomeNetOrm.Generators.Base
{
    // 🔥 Весь базовый CRUD теперь здесь! Чистый Fluent без хардкода полей.
   internal static class BaseCrudSqlGeneratorExtensions
    {
      

        extension<T>(ISqlGenerator<T> generator) where T : BaseEntity
        {
            public SqlQueryCommand GenerateInsert()
            {
                // 🔥 Кастуем к конкретному базовому классу вместо dynamic!
                BaseSqlGenerator<T> baseGen = (BaseSqlGenerator<T>)generator;

                TokenQueryBuilder<T> builder = generator.CreateQuery()
                    .Emit(Insert).Emit(Into).Table()
                    .InsertFields()
                    .Emit(Values)
                    .InsertParameters();

                // Теперь Translator пробивается со строгой типизацией
                string returningSql = baseGen.Translator.FormatReturningId("id");

                return builder.Raw(returningSql).Build();
            }

            public SqlQueryCommand GenerateUpdate()
            {
                // 🔥 Кастуем к конкретному базовому классу вместо dynamic!
                BaseSqlGenerator<T> baseGen = (BaseSqlGenerator<T>)generator;

                return generator.CreateQuery()
                    .Emit(Update).Table()
                    .Emit(Set).SetFields()
                    .Emit(Where).Col(e => e.Id).Emit(Equal).Param(e => e.Id, baseGen.DapperParams["Id"])
                    .Build();
            }

            public SqlQueryCommand GenerateSelectAll()
            {
                return generator.CreateQuery()
                    .Emit(Select).Fields().Emit(From).Table()
                    .Build();
            }

            public SqlQueryCommand GenerateDelete(Guid id)
            {
                return generator.CreateQuery()
                    .Emit(Delete).Emit(From).Table()
                    .Emit(Where).Col(e => e.Id).Emit(Equal).Param(e => e.Id, id)
                    .Build();
            }

            public SqlQueryCommand GenerateSelectById(Guid id)
            {
                return generator.CreateQuery()
                    .Emit(Select).Fields().Emit(From).Table()
                    .Emit(Where).Col(e => e.Id).Emit(Equal).Param(e => e.Id, id)
                    .Build();
            }
        }
    }
}
