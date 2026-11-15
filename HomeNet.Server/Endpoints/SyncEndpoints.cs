using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Dto;
using HomeNet.Models.Shared.Entity;
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
            var group = endpoints.MapGroup("/api/sync");

            // ============================================================================
            // 📥 PULL ПОЛЬЗОВАТЕЛЕЙ + НАДГРОБИЯ ПО ВЕРСИИ
            // ============================================================================
            group.MapGet("/users", async (Guid deviceId, long lastVersion) =>
            {
                try
                {
                    serverLogger.LogInfo($"[API SYNC] Запрос обновлений профилей с версии {lastVersion} (DeviceId: {deviceId})...");

                    var allUsers = await userRepository.GetAllAsync();
                    var updatedUsers = allUsers.Where(u => u.Version > lastVersion).ToList();

                    var tombstones = (await deletedRecordRepository.GetDeletedSinceVersionAsync("Users", lastVersion)).ToList();

                    long maxUserVersion = updatedUsers.Count > 0 ? updatedUsers.Max(u => u.Version) : lastVersion;
                    long maxDeletedVersion = tombstones.Count > 0 ? tombstones.Max(d => d.Version) : lastVersion;
                    long latestVersion = Math.Max(lastVersion, Math.Max(maxUserVersion, maxDeletedVersion));

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

            // ============================================================================
            // 📥 ВЕКТОР СИНХРОНИЗАЦИИ: Получаем ID пользователя, привязанного к этому девайсу
            // ============================================================================
            group.MapGet("/version/{deviceId:guid}", async (Guid deviceId) =>
            {
                serverLogger.LogInfo($"[API SYNC] Запрос вектора синхронизации для девайса: {deviceId}");
                Guid userId = await deviceSyncRepository.GetUserIdByDeviceAsync(deviceId);
                return Results.Ok(new { DeviceId = deviceId, UserId = userId });
            });

            // ============================================================================
            // 📤 ФИКСАЦИЯ ЯКОРЯ СИНХРОНИЗАЦИИ: Обновляем чекпоинт устройства в Postgres
            // ============================================================================
            group.MapPost("/anchor", async (DeviceSyncRequest request) =>
            {
                if (request == null || request.DeviceId == Guid.Empty || request.UserId == Guid.Empty)
                    return Results.BadRequest("Неполные данные для фиксации якоря.");

                serverLogger.LogInfo($"[API SYNC] Фиксация якоря репликации для девайса: {request.DeviceId}");
                await deviceSyncRepository.UpdateDeviceCheckpointAsync(
                    request.DeviceId,
                    request.UserId,
                    DateTimeOffset.UtcNow
                );

                return Results.Ok(new { Anchored = true });
            });
        }
    }

    public record DeviceSyncRequest(Guid DeviceId, Guid UserId);
}
