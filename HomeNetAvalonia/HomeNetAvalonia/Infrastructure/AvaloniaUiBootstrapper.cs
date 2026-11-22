using Dapper;
using HomeNet.DI;
using HomeNetAvalonia.Animators;
using HomeNetCore.Enums;
using HomeNetOrm.Helpers;
using System.Threading.Tasks;

namespace HomeNetAvalonia.Infrastructure
{
    public static class AvaloniaUiBootstrapper
    {
        private static bool _isInitialized = false;
       

        public static AppCompositionRoot BuildAvaloniaContainer(BackendMode mode, string apiBaseUrl, string sqliteConn)
        {
            if (_isInitialized) return AppCompositionRoot.Instance;

            // 1. Базовые мапперы типов Dapper
            SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
            SqlMapper.AddTypeHandler(new GuidTypeHandler());

            // 2. Строим каркас
            AppCompositionRoot.Initialize(mode, apiBaseUrl, sqliteConn);
            _isInitialized = true;

            var root = AppCompositionRoot.Instance;

           

            if (mode is BackendMode.Real or BackendMode.Local)
            {
                _ = new CloseWindowAnimator(root.EventBus);
                _ = new MinimizeWindowAnimator(root.EventBus);
                _ = new WidescreenWindowAnimator(root.EventBus);
            }

            // ============================================================================
            // 💥 СТАРТ ИНИЦИАЛИЗАЦИИ БД
            // ============================================================================
            Task.Run(async () =>
            {
                await root.DataAccess.DbContextContainer.InitializeAllDatabasesAsync();
            });

            return root;
        }

        

       

        
    }
}