using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetOrm.Interfaces.Builders;

namespace HomeNetOrm.Infrastructure
{
    public class DbContextContainer(
        IDbEngine localEngine,
        IEventBus eventBus,
        ICastomLogger logger) : IDbContextContainer
    {
        private readonly IDbEngine _localEngine = localEngine ?? throw new ArgumentNullException(nameof(localEngine));
        private readonly IEventBus _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        private readonly ICastomLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Чистая абстракция состояния локального подключения
        public bool IsLocalAvailable { get; private set; }

        public async Task InitializeAllDatabasesAsync()
        {
            _logger.LogInfo("=== [СУБД ХАБ] ЗАПУСК ИНИЦИАЛИЗАЦИИ ЛОКАЛЬНОГО ХРАНИЛИЩА ===");

            await InitializeLocalChannelInternalAsync();

            _logger.LogInfo("=== [СУБД ХАБ] ИНИЦИАЛИЗАЦИЯ ЗАВЕРШЕНА ===");
            _eventBus.Publish(this, new IDbContextContainer.InfrastructureReady());
        }

        private async Task InitializeLocalChannelInternalAsync()
        {
            try
            {
                await _localEngine.InitializeChannelAsync();
                IsLocalAvailable = true;
                _logger.LogInfo("[СУБД СТАТУС] Локальный движок данных успешно запущен.");
            }
            catch (Exception ex)
            {
                IsLocalAvailable = false;
                _logger.LogError($"[СУБД СТАТУС] Локальный движок недоступен: {ex.Message}");
            }
        }
    }
}
