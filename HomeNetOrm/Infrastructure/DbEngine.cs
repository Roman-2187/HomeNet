using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetOrm.DBProviders.Base;
using HomeNetOrm.DBProviders.Postgres;
using HomeNetOrm.DBProviders.Sqlite;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Interfaces.DbInfrastructure;
using HomeNetOrm.Models;
using HomeNetOrm.Repositories.Base;
using Microsoft.Data.Sqlite;
using Npgsql;
using System.Data;
using System.Data.Common;

namespace HomeNetOrm.Infrastructure
{
    public enum DbEngineType
    {
        SQLiteSQLClient, // Режим клиента (SQLite)
        PostgreSQLServer // Режим сервера (PostgreSQL)
    }

    public class DbEngine : IDbEngine, IDisposable
    {
        private readonly string _connectionString;
        private readonly ICastomLogger _logger;
        private readonly IEnumerable<TableSchema> _schemas;
        private readonly ISqlDialectTranslator _translator;
        private readonly IDbProviderSpecification _spec;
        private readonly ISchemaSqlGenerator _schemaGenerator;
        private readonly DbEngineType _engineType;

        // 🔥 Тип фабрики теперь общий, чтобы можно было хранить и Local, и Remote
        public BaseRepositoryFactory Repositories { get; private set; } = null!;

        public Func<DbConnection> ConnectionFactory => () =>
        {
            DbConnection conn = _engineType switch
            {
                DbEngineType.SQLiteSQLClient => new SqliteConnection(_connectionString),
                DbEngineType.PostgreSQLServer => new NpgsqlConnection(_connectionString),
                _ => throw new InvalidOperationException("Неизвестный тип СУБД движка.")
            };
            return conn;
        };

        public DbConnection Connection => ConnectionFactory();

        public DbEngine(DbEngineType engineType, string connectionString, IEnumerable<TableSchema> schemas, ICastomLogger logger)
        {
            _engineType = engineType;
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schemas = schemas ?? throw new ArgumentNullException(nameof(schemas));

            switch (engineType)
            {
                case DbEngineType.SQLiteSQLClient:
                    _translator = new SqliteDialectTranslator();
                    _spec = new SqliteProviderSpecification();
                    _schemaGenerator = new SqliteSchemaGenerator(_spec);
                    break;

                case DbEngineType.PostgreSQLServer:
                    _translator = new PostgresDialectTranslator();
                    _spec = new PostgresProviderSpecification();
                    _schemaGenerator = new PostgresSchemaSqlGenerator(_spec);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(engineType), "Неизвестный тип СУБД движка.");
            }
        }

        // =========================================================================
        // 🏗️ ИНИЦИАЛИЗАЦИЯ РЕПОЗИТОРИЕВ (Вызывается из DataAccessModule)
        // =========================================================================
        public void InitializeRepositories(IEventBus? eventBus)
        {
            _logger.LogInfo("[DbEngine] Инициализация фабрики репозиториев...");

            // 🔥 ЕДИНАЯ ТОЧКА ВЫБОРА ФАБРИКИ
            if (_engineType == DbEngineType.SQLiteSQLClient)
            {
                if (eventBus == null)
                    throw new ArgumentNullException(nameof(eventBus), "Клиентская фабрика требует IInternalEventBus!");

                Repositories = new LocalRepositoryFactory(
                    ConnectionFactory,
                    _schemas,
                    _translator,
                    _logger,
                    eventBus);
            }
            else
            {
                Repositories = new RemoteRepositoryFactory(
                    ConnectionFactory,
                    _schemas,
                    _translator,
                    _logger);
            }
        }

        // =========================================================================
        // ⚙️ УНИВЕРСАЛЬНЫЙ ЗАПУСК ИНИЦИАЛИЗАЦИИ ТАБЛИЦ И СХЕМ
        // =========================================================================
        public async Task InitializeChannelAsync()
        {
            using (var baseConnection = ConnectionFactory())
            {
                if (baseConnection.State != ConnectionState.Open)
                {
                    await baseConnection.OpenAsync();
                }

                var schemaProvider = new GenericSchemaProvider(_schemaGenerator, baseConnection, _spec, _logger);
                var tableInitializer = new SchemaRepository(baseConnection, schemaProvider, _schemaGenerator, _schemas, _logger);

                await tableInitializer.InitializeAsync();

                if (_engineType == DbEngineType.PostgreSQLServer)
                {
                    await tableInitializer.MigratePostgresVersionTriggersAsync(
                        baseConnection,
                        _schemaGenerator,
                        _translator,
                        _schemas,
                        _logger
                    );
                }
            }
        }

        public void Dispose()
        {
            // Если у фабрики есть ресурсы для очистки, очищаем здесь
        }
    }
}