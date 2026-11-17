using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNet.Server.Filters;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Extensions;
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

            // === [📥 PULL СИНХРОНИЗАЦИЯ СМС] ===
            group.MapGet("/sync", async (HttpContext httpContext, string lastSyncedAt) =>
            {
                try
                {
                    var session = (DeviceSyncEntity)httpContext.Items["CurrentSession"]!;

                    if (!DateTimeOffset.TryParse(lastSyncedAt, out var clientCheckpoint))
                        clientCheckpoint = DateTimeOffset.MinValue;

                    serverLogger.LogInfo($"[API PULL] Юзер {session.UserId} запрашивает свои СМС с метки {clientCheckpoint:HH:mm:ss}");

                    var freshMessages = await messageRepository.GetMessagesForSyncAsync(session.UserId, clientCheckpoint);
                    var messageList = freshMessages.ToList();

                    serverLogger.LogInfo($"[API Успех] Отдано {messageList.Count} СМС для пользователя {session.UserId} из PostgreSQL.");
                    return Results.Ok(messageList);
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

                    await hubContext.Clients.Group(incomingMessage.ReceiverId.ToString()).SendAsync("ReceiveMessage", incomingMessage);
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
