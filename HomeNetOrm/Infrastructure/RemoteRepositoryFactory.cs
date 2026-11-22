using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNetOrm.DBProviders.Postgres;
using HomeNetOrm.Generators.Remote;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Interfaces.Generators;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Models;
using HomeNetOrm.Repositories.Base;
using HomeNetOrm.Repositories.Remote;
using System.Data.Common;
// 🔥 Для PostgresExceptionHandler

namespace HomeNetOrm.Infrastructure
{
    public class RemoteRepositoryFactory(
        Func<DbConnection> connectionFactory,
        IEnumerable<TableSchema> schemas,
        ISqlDialectTranslator translator,
        ICastomLogger logger) : BaseRepositoryFactory(connectionFactory, schemas, translator, logger)
    {
        // 🔥 Создаем обработчик ошибок для Postgres
        private readonly IDbExceptionHandler _errorHandler = new PostgresExceptionHandler();

        internal override IUserRepo CreateUserRepository(IUserSqlGenerator generator)
            => new UserRepository(ConnectionFactory, generator, Logger, _errorHandler); // 🔥 Передаем обработчик

        internal override IMessageRepo CreateMessageRepository(IMessageSqlGenerator generator)
            => new MessageRepository(ConnectionFactory, generator, Logger, _errorHandler); // 🔥 Передаем обработчик

        protected override void RegisterSpecificRepositories()
        {
            var deviceSyncGen = new DeviceSyncSqlGenerator(Schemas, Translator, Logger);
            Repositories[typeof(DeviceSyncRepo)] = new DeviceSyncRepo(ConnectionFactory, deviceSyncGen);

            var deletedRecordGen = new DeletedRecordSqlGenerator(Schemas, Translator, Logger);
            Repositories[typeof(DeletedRecordRepository)] = new DeletedRecordRepository(ConnectionFactory, deletedRecordGen);
        }




    }
}