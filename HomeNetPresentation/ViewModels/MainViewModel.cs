using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;

using BaseNavigationState = HomeNetPresentation.Serviсes.BaseNavigationState;

namespace HomeNetPresentation.ViewModels
{
    public partial class MainViewModel
    {
        public MainViewModel(ICastomLogger logger, IEventBus eventBus, BaseNavigationState navigation)
        {
            ICastomLogger logger1 = logger ?? throw new ArgumentNullException(nameof(logger));
            IEventBus eventBus1 = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            eventBus1.Subscribe<WindowOpenedEvent>(e =>
            {
                logger1.LogInfo($"[PERF] Окно открыто через {e.ElapsedMs} мс");
            });
        }
    }


    public class WindowOpenedEvent
    {
        public long ElapsedMs { get; init; }
    }
}
