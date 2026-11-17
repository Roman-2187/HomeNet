using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Server.Endpoints;
using HomeNet.Server.Filters;
using HomeNetOrm.Infrastructure;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Remote;
using HomeNetOrm.Schemes;
using Microsoft.AspNetCore.SignalR;

namespace HomeNet.Server
{
    public partial class Program
    {
        private static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. 🪐 СОБИРАЕМ МНОГОСТВОЛЬНЫЙ ЛОГГЕР
            ICastomLogger serverLogger = new AggregateLogger(
                new ConsoleLogger()
            );

            serverLogger.LogInfo("=== [СУБД ХАБ СЕРВЕРА] СИСТЕМА ДВОЙНОГО ЛОГИРОВАНИЯ ИНИЦИАЛИЗИРОВАНА ===");

            var schemaModule = new RemoteMessengerSchemaModule();
            var serverSchemas = schemaModule.GetSchemas();

            builder.Configuration.AddJsonFile("secrets.json", optional: true, reloadOnChange: true);

            string postgresConn = builder.Configuration["PostgresConnection"]
                ?? throw new InvalidOperationException("В secrets.json отсутствует PostgresConnection");

            // 2. Создаем движок для PostgreSQL
            var postgresEngine = new SiberNetDbEngine(DbEngineType.PostgreSQLServer, postgresConn, serverSchemas, serverLogger);

            // 🔥 3. Инициализируем фабрику репозиториев БЕЗ шины событий (null)
            postgresEngine.InitializeRepositories(null);

            // 🔥 4. Достаем готовые репозитории из фабрики
            IMessageRepo messageRepository = postgresEngine.Repositories.GetRepository<IMessageRepo>();
            IUserRepo userRepository = postgresEngine.Repositories.GetRepository<IUserRepo>();
            DeviceSyncRepository deviceSyncRepository = postgresEngine.Repositories.GetRepository<DeviceSyncRepository>();
            DeletedRecordRepository deletedRecordRepository = postgresEngine.Repositories.GetRepository<DeletedRecordRepository>();

            // 🔥 5. РЕГИСТРИРУЕМ ЗАВИСИМОСТИ И ФИЛЬТР АВТОРИЗАЦИИ В DI ASP.NET CORE:
            builder.Services.AddSingleton(userRepository);
            builder.Services.AddSingleton(deletedRecordRepository);
            builder.Services.AddSingleton(deviceSyncRepository);
            builder.Services.AddSingleton(serverLogger);
            builder.Services.AddSingleton<SessionAuthFilter>();

            builder.Services.AddSignalR();
            var app = builder.Build();

            // --- МАППИНГ РАЗДЕЛЕННЫХ СЛОЕВ ЭНДПОИНТОВ ---
            app.MapMessageEndpoints(messageRepository, serverLogger);
            app.MapUserEndpoints(userRepository, deviceSyncRepository, serverLogger);
            app.MapSyncEndpoints(deviceSyncRepository, userRepository, deletedRecordRepository, serverLogger);

            app.MapGet("/api/health", () =>
            {
                serverLogger.LogInfo("[HealthCheck] Клиент проверил доступность сервера.");

                return Results.Ok(new
                {
                    Status = "Healthy",
                    ServerTime = DateTimeOffset.UtcNow,
                    Message = "HomeNet Server is running"
                });
            }).AllowAnonymous();

            // Сетевой автобус SignalR
            app.MapHub<ServerChatHub>("/chatHub");

            // Инициализация базы данных строго перед стартом самого веб-сервера!
            serverLogger.LogInfo("=== [СУБД ХАБ СЕРВЕРА] ЗАПУСК ИНИЦИАЛИЗАЦИИ POSTGRES ===");
            await postgresEngine.InitializeChannelAsync();

            serverLogger.LogInfo("[SERVER START] Веб-сервер запущен и слушает порт.");
            app.Run();
        }
    }

    public class ServerChatHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            string? userId = Context.GetHttpContext()?.Request.Query["userId"];
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }

            // 🔥 РУКОПОЖАТИЕ SIBERNET:
            await Clients.Caller.SendAsync("ReceiveServerReady");

            await base.OnConnectedAsync();
        }
    }
}
