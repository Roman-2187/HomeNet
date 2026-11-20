using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Dto;
using HomeNet.Models.Shared.Entity;
using HomeNet.Server.Filters;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Local.Sync;
using Microsoft.AspNetCore.SignalR;

namespace HomeNet.Server.Endpoints
{
    public static class MessageEndpoints
    {
        public static void MapMessageEndpoints(this IEndpointRouteBuilder endpoints, IMessageRepo messageRepository, ICastomLogger serverLogger)
        {
            // Вешаем замок X-Session-Id на ВСЕ эндпоинты сообщений!
            var group = endpoints.MapGroup("/api/messages")
                                 .AddEndpointFilter<SessionAuthFilter>();

            // === [📥 PULL СИНХРОНИЗАЦИЯ СМС ПО ВЕРСИИ] ===
            group.MapGet("/sync", async (HttpContext httpContext, long sinceVersion = 0) =>
            {
                try
                {
                    var session = (DeviceSyncEntity)httpContext.Items["CurrentSession"]!;

                    serverLogger.LogInfo($"[API PULL] Юзер {session.UserId} запрашивает свои СМС с версии {sinceVersion}...");

                    var freshMessages = (await messageRepository.GetMessagesSinceVersionAsync(session.UserId, sinceVersion)).ToList();
                    long latestVersion = freshMessages.Count > 0 ? freshMessages.Max(m => m.Version) : sinceVersion;

                    var batch = new SyncBatchResponse<MessageEntity>
                    {
                        UpdatedItems = freshMessages,
                        DeletedIds = [],
                        LatestVersion = latestVersion
                    };

                    serverLogger.LogInfo($"[API Успех] Отдано {freshMessages.Count} СМС для пользователя {session.UserId}. Новая версия: {latestVersion}.");
                    return Results.Ok(batch);
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API Ошибка] Сбой PULL синхронизации СМС: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
            });

            // === [📤 PUSH ПРИЕМ СООБЩЕНИЙ] ===
            group.MapPost("/", async (HttpContext httpContext, MessageEntity incomingMessage, IHubContext<ServerChatHub> hubContext) =>
            {
                try
                {
                    var session = (DeviceSyncEntity)httpContext.Items["CurrentSession"]!;

                    // Защита от подмены отправителя!
                    if (incomingMessage.SenderId != session.UserId)
                    {
                        serverLogger.LogWarning($"[API БЕЗОПАСНОСТЬ] Попытка подмены SenderId ({incomingMessage.SenderId} != {session.UserId})!");
                        return Results.Forbid();
                    }

                    serverLogger.LogInfo($"[API] Прилетел объект сообщения от {incomingMessage.SenderId} для {incomingMessage.ReceiverId}");
                    await messageRepository.InsertAsync(incomingMessage);


                    var receiverGroup = incomingMessage.ReceiverId.ToString();
                    var senderGroup = incomingMessage.SenderId.ToString();

                    await hubContext.Clients.Group(receiverGroup).SendAsync("ReceiveMessage", incomingMessage);

                    if (receiverGroup != senderGroup)
                    {
                        await hubContext.Clients.Group(senderGroup).SendAsync("ReceiveMessage", incomingMessage);
                    }


                    return Results.Ok(new { Success = true });
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API Ошибка] Не удалось сохранить через репозиторий: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
            });

            group.MapGet("/", () => Results.Ok(new { Status = "OK" }));
        }
    }
}
