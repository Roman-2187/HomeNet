using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Dto;
using HomeNet.Models.Shared.Entity;
using HomeNet.Server.Filters;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Remote;

namespace HomeNet.Server.Endpoints
{
    public static class SyncEndpoints
    {
        public static void MapSyncEndpoints(
            this IEndpointRouteBuilder endpoints,
            DeviceSyncRepository deviceSyncRepository,
            IUserRepo userRepository,
            DeletedRecordRepository deletedRecordRepository,
            ICastomLogger serverLogger)
        {
            // Вешаем фильтр проверки X-Session-Id на всю группу синхронизации!
            var group = endpoints.MapGroup("/api/sync")
                                 .AddEndpointFilter<SessionAuthFilter>();

            // ============================================================================
            // 📥 PULL ПОЛЬЗОВАТЕЛЕЙ + НАДГРОБИЯ ПО СЕРВЕРНОМУ КУРСОРУ ВЕРСИЙ
            // ============================================================================
            group.MapGet("/users", async (HttpContext httpContext) =>
            {
                try
                {
                    var session = (DeviceSyncEntity)httpContext.Items["CurrentSession"]!;
                    long lastVersion = session.LastVersion;

                    serverLogger.LogInfo($"[API SYNC] Запрос профилей с версии {lastVersion} (SessionId: {session.Id}, UserId: {session.UserId})...");

                    var allUsers = await userRepository.GetAllAsync();
                    var updatedUsers = allUsers.Where(u => u.Version > lastVersion).ToList();

                    // Затираем пароли перед отправкой по сети
                    foreach (var u in updatedUsers)
                    {
                        u.Password = string.Empty;
                    }

                    var tombstones = (await deletedRecordRepository.GetDeletedSinceVersionAsync("Users", lastVersion)).ToList();

                    long maxUserVersion = updatedUsers.Count > 0 ? updatedUsers.Max(u => u.Version) : lastVersion;
                    long maxDeletedVersion = tombstones.Count > 0 ? tombstones.Max(d => d.Version) : lastVersion;
                    long latestVersion = Math.Max(lastVersion, Math.Max(maxUserVersion, maxDeletedVersion));

                    // Двигаем курсор версии прямо в таблице device_sync на сервере
                    if (latestVersion > lastVersion)
                    {
                        await deviceSyncRepository.UpdateLastVersionAsync(session.Id, latestVersion);
                    }

                    var batch = new SyncBatchResponse<UserEntity>
                    {
                        UpdatedItems = updatedUsers,
                        DeletedIds = tombstones.Select(t => t.Id).ToList(),
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
