using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Synchronization.Interfaces.Sinc;
using static HomeNetCore.Interfaces.ViewModels.UserZone.IDeviceSettingsVM;

namespace HomeNet.Synchronization.Sinc
{
    public class SyncBatchFactory : IDisposable
    {
        private readonly IEnumerable<IEntitySyncItem> _items;
        private readonly IEventBus _eventBus;
        private readonly ICastomLogger _logger;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public SyncBatchFactory(
            IEnumerable<IEntitySyncItem> items,
            IEventBus eventBus,
            ICastomLogger logger)
        {
            _items = items.OrderBy(i => i.Order).ToList();
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _eventBus.Subscribe<SessionPersisted>(OnSessionPersisted);
        }

        public async Task RunAllAsync(CancellationToken ct = default)
        {
            if (!await _lock.WaitAsync(0, ct))
            {
                
                return;
            }

            try
            {
                _logger.LogInfo("=== [SyncFactory] ЗАПУСК ПАКЕТНОЙ СИНХРОНИЗАЦИИ ===");

                foreach (var item in _items)
                {
                    await item.PullAsync(ct);
                    await item.PushAsync(ct);
                }

                _logger.LogInfo("=== [SyncFactory] ПАКЕТНАЯ СИНХРОНИЗАЦИЯ ЗАВЕРШЕНА ===");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[SyncFactory] Критический сбой: {ex.Message}");
            }
            finally
            {
                _lock.Release();
            }
        }

        private async void OnSessionPersisted(SessionPersisted e)
        {
            await RunAllAsync();
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<SessionPersisted>(OnSessionPersisted);
            _lock.Dispose();
        }
    }
}
