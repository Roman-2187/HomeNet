using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;

namespace HomeNetPresentation.ViewModels.MainViews
{
    public class MainViewModel:FormViewModelBase
    {
        public MainViewModel(ICastomLogger logger, IEventBus eventBus) : base(eventBus, logger)
        {
            
             

            EventBus.Subscribe<WindowOpenedEvent>(e =>
            {
                Logger.LogInfo($"[PERF] Окно открыто через {e.ElapsedMs} мс");
            });
        }
    }


    public class WindowOpenedEvent
    {
        public long ElapsedMs { get; init; }
    }
}
