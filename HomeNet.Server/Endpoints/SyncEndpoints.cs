using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Dto;
using HomeNet.Models.Shared.Entity;
using HomeNet.Server.Filters;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Local.Sync;
using HomeNetOrm.Repositories.Remote;

namespace HomeNet.Server.Endpoints
{
    public static class SyncEndpoints
    {
        public static void MapSyncEndpoints(
            this IEndpointRouteBuilder endpoints,
            IUserRepo userRepository,
            DeletedRecordRepository deletedRecordRepository,
            ICastomLogger serverLogger)
        {
            // Вешаем фильтр проверки X-Session-Id на всю группу синхронизации!
            var group = endpoints.MapGroup("/api/sync")
                                 .AddEndpointFilter<SessionAuthFilter>();

            // ============================================================================
            // 📥 PULL ПОЛЬЗОВАТЕЛЕЙ + НАДГРОБИЯ ПО КЛИЕНТСКОМУ КУРСОРУ ВЕРСИЙ
            // ============================================================================
            group.MapGet("/users", async (HttpContext httpContext, long sinceVersion = 0) =>
            {
                try
                {
                    var session = (DeviceSyncEntity)httpContext.Items["CurrentSession"]!;

                    serverLogger.LogInfo($"[API SYNC] Запрос профилей с версии {sinceVersion} (SessionId: {session.Id}, UserId: {session.UserId})...");

                    var updatedUsers = (await userRepository.GetUsersSinceVersionAsync(sinceVersion)).ToList();

                    // Затираем пароли перед отправкой по сети
                    foreach (var u in updatedUsers)
                    {
                        u.Password = string.Empty;
                    }

                    var tombstones = (await deletedRecordRepository.GetDeletedSinceVersionAsync("Users", sinceVersion)).ToList();

                    var maxUserVersion = updatedUsers.Count > 0 ? updatedUsers.Max(u => u.Version) : sinceVersion;
                    var maxDeletedVersion = tombstones.Count > 0 ? tombstones.Max(d => d.Version) : sinceVersion;
                    var latestVersion = Math.Max(sinceVersion, Math.Max(maxUserVersion, maxDeletedVersion));

                    var batch = new SyncBatchResponse<UserEntity>
                    {
                        UpdatedItems = updatedUsers,
                        DeletedIds = [.. tombstones.Select(t => t.Id)],
                        LatestVersion = latestVersion
                    };

                    serverLogger.LogInfo($"[API SYNC] Отдано {updatedUsers.Count} профилей и {batch.DeletedIds.Count} надгробий. Новая версия: {latestVersion}.");
                    return Results.Ok(batch);
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API SYNC] Сбой PULL пользователей: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
            });
        }
    }
}
