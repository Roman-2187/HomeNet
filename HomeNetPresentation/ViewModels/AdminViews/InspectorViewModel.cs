using CommunityToolkit.Mvvm.ComponentModel;
using HomeNet.Infrastructure.Shared.Diagnostics.EventInspector;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.ViewModels.AdminZone;


namespace HomeNetPresentation.ViewModels.AdminViews
{
    public partial class InspectorViewModel : MainViews.FormViewModelBase
    {
        private readonly EventBusInspector _inspector;

        // 🔥 Запоминаем контекст UI-потока при создании ViewModel
        private readonly SynchronizationContext? _uiContext = SynchronizationContext.Current;

        [ObservableProperty]
        public partial List<IEventInspector.Line> GraphLines { get; set; } = [];
        [ObservableProperty]
        public partial bool IsScanning { get; set; }

        [ObservableProperty]
        public partial string LoadingMessage { get; set; } = string.Empty;

        public InspectorViewModel(IEventBus eventBus,ICastomLogger logger, IEventInspector inspector)
            : base(eventBus,logger)
        {
            _inspector = inspector as EventBusInspector ?? throw new ArgumentNullException(nameof(inspector));

            // 🔥 Подписываемся на событие сканирования
            _inspector.ScanCompleted += OnScanCompleted;

            EventBus.Subscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
            UpdateReport();
        }

        private void OnScanCompleted()
        {
            // 🔥 Возвращаемся в UI-поток и обновляем граф
            if (_uiContext != null)
            {
                _uiContext.Post(_ => UpdateReport(), null);
            }
            else
            {
                // Если контекст не найден (например, в тестах), обновляем напрямую
                UpdateReport();
            }
        }

        private void OnAdminTabChanged(IAdminVm.AdminTabChanged? msg)
        {
            if (msg == null) return;

            if (msg.ActiveTab == AdminSubTab.EventInspector)
            {
                UpdateReport();
            }
        }

        private void UpdateReport()
        {
            // 🔥 Проверяем, идет ли сканирование
            if (_inspector.IsScanning)
            {
                IsScanning = true;
                LoadingMessage = "⌛ Идёт сканирование сборок... Пожалуйста, подождите.";
                GraphLines = [];
                return;
            }

            // Если сканирование завершено, показываем граф
            IsScanning = false;
            LoadingMessage = string.Empty;
            GraphLines = _inspector.GenerateObjectGraph();
        }

        public override void Dispose()
        {
            base.Dispose();
            _inspector.ScanCompleted -= OnScanCompleted; // 🔥 Отписываемся
            EventBus.Unsubscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
        }
    }
}