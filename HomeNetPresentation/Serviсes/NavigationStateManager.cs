using CommunityToolkit.Mvvm.ComponentModel;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNetCore.Enums.Navigation;

namespace HomeNetPresentation.Serviсes
{
    public partial class BaseNavigationState(IEventBus eventBus) : ObservableObject, IDisposable
    {
        protected readonly IEventBus EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

        // Единое сквозное свойство для привязки UI и подгрузки ID
        [ObservableProperty] protected partial MainTab CurrentMainZone { get; set; } = MainTab.StartZone;
        [ObservableProperty]
        public partial UserEntity? CurrentUser { get; set; }

        public virtual void Dispose() { }
    }
}
