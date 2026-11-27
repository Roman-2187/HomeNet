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
        public DbEngine DbEngine { get; }
        public string ApiBaseUrl { get; }
        public IDbContextContainer DbContextContainer { get; }
        public IUserRepo UserRepository { get; }
        public IMessageRepo MessageRepository { get; }
        public DeviceSettingsRepo DeviceSettingsRepository { get; }

        // 🔥 Единый клиент SignalR вместо двух отдельных слушателей
        public SignalRHubClient HubClient { get; }

        public IUserApiClient UserApiClient { get; }
        public IMessageApiClient MessageApiClient { get; }

        public SyncBatchFactory SyncFactory { get; }

        public UserSyncService UserSyncService { get; }
        public MessageSyncService MessageSyncService { get; }

        public DataAccessModule(
            string sqliteConn, string apiBaseUrl,
            IEnumerable<TableSchema> appSchemas,
            ICastomLogger logger, ILogQueueManager logQueue,
            IEventBus eventBus)
        {
            ApiBaseUrl = apiBaseUrl;

            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

            // 1. Движок и репозитории
            DbEngine = new DbEngine(DbEngineType.SQLiteSQLClient, sqliteConn, appSchemas, logger);
            DbEngine.InitializeRepositories(eventBus);
            UserRepository = DbEngine.Repositories.GetRepository<IUserRepo>();
            MessageRepository = DbEngine.Repositories.GetRepository<IMessageRepo>();
            DeviceSettingsRepository = DbEngine.Repositories.GetRepository<DeviceSettingsRepo>();

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

            var serverHealthMonitor = new ServerHealthMonitor(httpClient, logger, eventBus);

            // 3. API-клиенты
            UserApiClient = new UserApiClient(httpClient, logger, eventBus);
            MessageApiClient = new MessageApiClient(httpClient, logger, eventBus);

            // 4. Контейнер хаба
            DbContextContainer = new DbContextContainer(DbEngine, eventBus, logger);

            // 5. Пакетная синхронизация
            var syncItems = new IEntitySyncItem[]
            {
                new UserSyncItem(UserRepository, DeviceSettingsRepository, UserApiClient, logger),
                new MessageSyncItem(MessageRepository, DeviceSettingsRepository, MessageApiClient, logger,eventBus)
            };
            SyncFactory = new SyncBatchFactory(syncItems, eventBus, logger);

            // 6. Слушатели реального времени (без API-клиентов)
            UserSyncService = new UserSyncService(UserRepository, eventBus, logger);
            MessageSyncService = new MessageSyncService(MessageRepository, eventBus, logger);

            // 7. Единый SignalR-хаб с набором обработчиков
            var hubHandlers = new IHubHandler[]
            {
                new UserHubHandler(logger, eventBus),
                new MessageHubHandler(logger, eventBus)
            };
            HubClient = new SignalRHubClient(apiBaseUrl, logger, eventBus, hubHandlers);
        }

        // Корректная очистка ресурсов при закрытии приложения
        public async ValueTask DisposeAsync()
        {
            SyncFactory.Dispose();
            UserSyncService.Dispose();
            MessageSyncService.Dispose();
            await HubClient.DisposeAsync();
        }
    }
}
