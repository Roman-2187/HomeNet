using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Network.Api;
using HomeNet.Network.ApiClient;
using HomeNet.Network.Handlers;
using HomeNet.Network.SignalRListeners;
using HomeNet.Synchronization.Interfaces.Sinc;
using HomeNet.Synchronization.Items;
using HomeNet.Synchronization.Sinc;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Infrastructure;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Models;
using HomeNetOrm.Repositories.Local;

namespace HomeNet.DI.Modules
{
    public class DataAccessModule : IAsyncDisposable
    {
        public SiberNetDbEngine DbEngine { get; }
        public string ApiBaseUrl { get; }
        public IDbContextContainer DbContextContainer { get; }
        public IUserRepo UserRepository { get; }
        public IMessageRepo MessageRepository { get; }
        public ClientDeviceSettingsRepository DeviceSettingsRepository { get; }
        public Guid CurrentDeviceId { get; private set; }

        public ServerHealthMonitor HealthMonitor { get; }

        public UserSignalRListener UserListener { get; }
        public MessageSignalRListener MessageListener { get; }

        public IUserApiClient UserApiClient { get; }
        public IMessageApiClient MessageApiClient { get; }

        // 🔥 Новая фабрика пакетной синхронизации вместо OrmEngine
        public SyncBatchFactory SyncFactory { get; }

        // 🔥 Легкие слушатели реального времени
        public UserSyncService UserSyncService { get; }
        public MessageSyncService MessageSyncService { get; }

        public DataAccessModule(
            string sqliteConn, string apiBaseUrl,
            IEnumerable<TableSchema> appSchemas,
            ICastomLogger logger, ILogQueueManager logQueue,
            IEventBus eventBus)
        {
            ApiBaseUrl = apiBaseUrl;

            // Перенесли настройку Dapper из удаленного CompositeSyncCoordinator
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

            // 1. Движок и репозитории
            DbEngine = new SiberNetDbEngine(DbEngineType.SQLiteSQLClient, sqliteConn, appSchemas, logger);
            DbEngine.InitializeRepositories(eventBus);
            UserRepository = DbEngine.Repositories.GetRepository<IUserRepo>();
            MessageRepository = DbEngine.Repositories.GetRepository<IMessageRepo>();
            DeviceSettingsRepository = DbEngine.Repositories.GetRepository<ClientDeviceSettingsRepository>();

            // 2. HttpClient с перехватчиком состояния сети
            var statusHandler = new ConnectionStatusHandler(eventBus)
            {
                InnerHandler = new HttpClientHandler()
            };

            var httpClient = new HttpClient(statusHandler)
            {
                BaseAddress = new Uri(apiBaseUrl),
                Timeout = TimeSpan.FromSeconds(10)
            };

            HealthMonitor = new ServerHealthMonitor(httpClient, logger, eventBus);

            // 3. API-клиенты
            UserApiClient = new UserApiClient(httpClient, logger, eventBus);
            MessageApiClient = new MessageApiClient(httpClient, logger, eventBus);

            // 4. Контейнер хаба
            DbContextContainer = new DbContextContainer(DbEngine, eventBus, logger);

            // 5. Пакетная синхронизация (Pull / Push по очереди через фабрику)
            var syncItems = new IEntitySyncItem[]
            {
                new UserSyncItem(UserRepository, DeviceSettingsRepository, UserApiClient, logger),
                new MessageSyncItem(MessageRepository, DeviceSettingsRepository, MessageApiClient, logger)
            };
            SyncFactory = new SyncBatchFactory(syncItems, eventBus, logger);

            // 6. Слушатели реального времени (без API-клиентов)
            UserSyncService = new UserSyncService(UserRepository, eventBus, logger);
            MessageSyncService = new MessageSyncService(MessageRepository, eventBus, logger);

            // 7. SignalR-листенеры
            UserListener = new UserSignalRListener(apiBaseUrl, logger, eventBus);
            MessageListener = new MessageSignalRListener(apiBaseUrl, logger, eventBus);
        }

        // Корректная очистка ресурсов при закрытии приложения
        public async ValueTask DisposeAsync()
        {
            SyncFactory.Dispose();
            UserSyncService.Dispose();
            MessageSyncService.Dispose();
            await UserListener.DisposeAsync();
            await MessageListener.DisposeAsync();
        }
    }
}
