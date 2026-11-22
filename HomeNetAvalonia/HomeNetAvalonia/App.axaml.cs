using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HomeNet.DI;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetAvalonia.Infrastructure;
using HomeNetAvalonia.MainViews;
using HomeNetCore.Enums;
using HomeNetOrm.Helpers;
using HomeNetPresentation.ViewModels;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace HomeNetAvalonia
{
    public partial class App : Application
    {
        private AppCompositionRoot? _compositionRoot;

        public IEventBus EventBus { get; private set; } = null!;

        public static readonly System.Diagnostics.Stopwatch AppStopwatch = System.Diagnostics.Stopwatch.StartNew();

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            bool isPreviewer = Avalonia.Controls.Design.IsDesignMode 
                               || Environment.GetCommandLineArgs().Any(arg => arg.Contains("preview", StringComparison.OrdinalIgnoreCase));

            if (isPreviewer)
            {
                base.OnFrameworkInitializationCompleted();
                return;
            }

            AppStopwatch.Restart();

            try
            {
                // 1. Читаем конфиг строго по конфигурации сборки (Debug / Release)
                var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory);

#if DEBUG
                // В режиме Debug грузим ТОЛЬКО локальный конфиг (localhost:5210)
                // optional: false сразу предупредит, если файл не скопировался в папку bin/Debug
                configBuilder.AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);
#else
                // В режиме Release грузим ТОЛЬКО боевой конфиг (VPS)
                configBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
#endif

                var config = configBuilder.Build();

                // 2. Берём значения (если нет — кидаем исключение, чтобы не было "молчаливого" хардкода)
                string apiBaseUrl = config["ApiBaseUrl"]
                    ?? throw new InvalidOperationException("В конфиге отсутствует ApiBaseUrl");

                string databaseName = config["DatabaseName"]
                    ?? throw new InvalidOperationException("В конфиге отсутствует DatabaseName");

                string backendModeStr = config["BackendMode"]
                    ?? throw new InvalidOperationException("В конфиге отсутствует BackendMode");

                BackendMode currentMode = Enum.Parse<BackendMode>(backendModeStr);

                // 3. Путь к базе (только имя, путь строится внутри)
                string dbPath = DatabasePathHelper.GetDatabasePath(databaseName);
                string sqliteConnectionString = $"Data Source={dbPath}";

                // 4. Запускаем сборщик
                _compositionRoot = AvaloniaUiBootstrapper.BuildAvaloniaContainer(
                    currentMode,
                    apiBaseUrl,
                    sqliteConnectionString);

                EventBus = _compositionRoot.EventBus;

                // Сразу пишем в лог, какой сервер выбран на этом запуске
                _compositionRoot.Logger.LogInfo($"[Конфиг] Активный сервер: {apiBaseUrl} | База: {dbPath}");

                // 5. Инициализируем окно и даём ему ViewModel
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = _compositionRoot.GetViewModel<MainViewModel>()
                    };

                    _compositionRoot.EventBusInspector.StartBackgroundScan(_compositionRoot.Logger);
                }

                if (currentMode == BackendMode.Real)
                {
                    _compositionRoot.Logger.LogInfo("[App Старт] запуск СУБД движков и координатора репликации успешно запущен в фоне.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критический сбой инициализации в Авалонии:\n\n{ex.Message}");
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown();
                }
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
