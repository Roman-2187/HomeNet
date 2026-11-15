using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Network.Api;
using HomeNet.Network.ApiClient;
using HomeNet.Network.Handlers; // 🔥 Не забудь подключить namespace твоего хендлера
using HomeNet.Synchronization;
using HomeNet.Synchronization.Interfaces.Sinc;
using HomeNet.Synchronization.Sinc;
using HomeNetCore.Interfaces.Diagnostics;
using HomeNetOrm.Infrastructure;
using HomeNetOrm.Interfaces.Builders;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Models;
using HomeNetOrm.Repositories.Local;

namespace HomeNet.DI.Modules
{
    public class DataAccessModule
    {
        public SiberNetDbEngine DbEngine { get; }
        public string ApiBaseUrl { get; }
        public IDbContextContainer DbContextContainer { get; }
        public IUserRepo UserRepository { get; }
        public IMessageRepo MessageRepository { get; }
        public ClientDeviceSettingsRepository DeviceSettingsRepository { get; }
        public Guid CurrentDeviceId { get; private set; }
        public SyncEngine OrmEngine { get; }

        public ServerHealthMonitor HealthMonitor { get; }

        public UserSignalRListener UserListener { get; }
        public MessageSignalRListener MessageListener { get; }

        public IUserApiClient UserApiClient { get; }
        public IMessageApiClient MessageApiClient { get; }

        public UserSyncService UserSyncService { get; }
        public MessageSyncService MessageSyncService { get; }

        public DataAccessModule(
            string sqliteConn, string apiBaseUrl,
            IEnumerable<TableSchema> appSchemas,
            ICastomLogger logger, ILogQueueManager logQueue,
            IEventBus eventBus)
        {
            ApiBaseUrl = apiBaseUrl;

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

            // 3. API-клиенты (теперь оба работают через перехватчик!)
            UserApiClient = new UserApiClient(httpClient, logger, eventBus);
            MessageApiClient = new MessageApiClient(httpClient, logger, eventBus);

            // 4. Контейнер хаба (чистый, только для SQLite)
            DbContextContainer = new DbContextContainer(DbEngine, eventBus, logger);

            // 5. Создаем SyncService
            UserSyncService = new UserSyncService(UserRepository, UserApiClient, eventBus, logger);
            MessageSyncService = new MessageSyncService(MessageRepository, MessageApiClient, eventBus, logger);

            // 6. Запускаем стартовую синхронизацию
            var syncCoordinator = new CompositeSyncCoordinator(UserSyncService, MessageSyncService);
            OrmEngine = new SyncEngine(UserRepository, MessageRepository, syncCoordinator, logger);

            // 7. SignalR-листенеры
            UserListener = new UserSignalRListener(apiBaseUrl, logger, eventBus);
            MessageListener = new MessageSignalRListener(apiBaseUrl, logger, eventBus);
        }

        


       



    }

    public class CompositeSyncCoordinator : ISyncCoordinator
    {
        private readonly ISyncCoordinator _userSync;
        private readonly ISyncCoordinator _messageSync;

        public CompositeSyncCoordinator(ISyncCoordinator userSync, ISyncCoordinator messageSync)
        {
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
            _userSync = userSync ?? throw new ArgumentNullException(nameof(userSync));
            _messageSync = messageSync ?? throw new ArgumentNullException(nameof(messageSync));
        }

        public async Task StartAsync()
        {
            await Task.WhenAll(_userSync.StartAsync(), _messageSync.StartAsync());
        }
    }
}
