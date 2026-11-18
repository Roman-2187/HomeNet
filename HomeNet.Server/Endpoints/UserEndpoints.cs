using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNet.Server.Filters;
using HomeNetOrm.Exeptions;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Remote;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace HomeNet.Server.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(
            this IEndpointRouteBuilder endpoints,
            IUserRepo userRepository,
            DeviceSyncRepository deviceSyncRepository,
            ICastomLogger serverLogger)
        {
            // =========================================================================
            // 🔓 ПУБЛИЧНЫЕ РУЧКИ (Работают без X-Session-Id и выдают его при успехе)
            // =========================================================================

            // === [🔐 АУТЕНТИФИКАЦИЯ] ===
            endpoints.MapPost("/api/users/auth", async (LoginRequest request) =>
            {
                try
                {
                    if (request.SessionId == Guid.Empty)
                    {
                        serverLogger.LogWarning($"[API AUTH] Отклонено: не передан SessionId для {request.Email}");
                        return Results.BadRequest(new { Error = "SessionId устройства обязателен для входа." });
                    }

                  
                    var user = await userRepository.GetByEmailAsync(request.Email);

                    if (user == null || user.Password != request.Password)
                    {
                        serverLogger.LogWarning($"[API AUTH] Неверный email или пароль для {request.Email}");
                        return Results.Unauthorized();
                    }

                    // 1. Фиксируем или продлеваем переданную клиентом сессию в таблице device_sync
                    await deviceSyncRepository.UpsertSessionAsync(request.SessionId, user.Id);

                    // 2. Возвращаем пользователя с подтвержденным токеном сессии, но без пароля
                    user.SessionId = request.SessionId;
                    user.Password = string.Empty;

                    serverLogger.LogInfo($"[API AUTH] Успешный вход для {request.Email} (SessionId: {request.SessionId})");
                    return Results.Ok(user);
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API AUTH] Ошибка авторизации: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
            });

            // === [📤 РЕГИСТРАЦИЯ НОВОГО ПОЛЬЗОВАТЕЛЯ] ===
            endpoints.MapPost("/api/users", async (UserEntity incomingUser, IHubContext<ServerChatHub> hubContext) =>
            {
                try
                {
                    if (incomingUser.SessionId == Guid.Empty)
                    {
                        serverLogger.LogWarning($"[API PUSH] Отклонена регистрация '{incomingUser.Email}': отсутствует SessionId.");
                        return Results.BadRequest(new { Error = "SessionId устройства обязателен для регистрации." });
                    }

                    if (incomingUser.Id == Guid.Empty)
                    {
                        serverLogger.LogWarning($"[API PUSH] Отклонена регистрация '{incomingUser.Email}': передан пустой Id пользователя.");
                        return Results.BadRequest(new { Error = "Идентификатор пользователя (Id) обязателен для регистрации." });
                    }


                    serverLogger.LogInfo($"[API PUSH] Регистрация пользователя '{incomingUser.Email}' (ID: {incomingUser.Id})");

                    // 1. Записываем нового пользователя в PostgreSQL
                    await userRepository.InsertAsync(incomingUser);

                    // 2. Открываем сессию в таблице device_sync строго с клиентским SessionId
                    await deviceSyncRepository.UpsertSessionAsync(incomingUser.SessionId, incomingUser.Id);

                    // 3. Подготавливаем безопасный ответ (без пароля)
                    incomingUser.Password = string.Empty;

                    serverLogger.LogInfo($"[API Успех] Новый пользователь '{incomingUser.FirstName}' записан. SessionId: {incomingUser.SessionId}");
                    return Results.Ok(incomingUser);
                }
                catch (DuplicateEmailException ex)
                {
                    serverLogger.LogWarning($"[API Конфликт] {ex.Message}");
                    return Results.Conflict(new { Error = ex.Message });
                }
                catch (DatabaseException ex)
                {
                    serverLogger.LogError($"[API Ошибка БД] {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API Ошибка] Не удалось зарегистрировать: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
            });

            // === [📥 ПРОВЕРКА СУЩЕСТВОВАНИЯ EMAIL] ===
            endpoints.MapGet("/api/users/check-email", async ([FromServices] IUserRepo repo, [FromQuery] string email) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(email)) return Results.BadRequest("Email обязателен");

                    var exists = await repo.EmailExistsAsync(email);
                    serverLogger.LogInfo($"[API] Проверка Email '{email}': {(exists ? "Занят" : "Свободен")}");
                    return Results.Ok(new { Exists = exists });
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API Ошибка] Сбой проверки Email: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
            });

            // =========================================================================
            // 🔒 ЗАЩИЩЁННЫЕ РУЧКИ (Требуют живой X-Session-Id в таблице device_sync)
            // =========================================================================
            var protectedGroup = endpoints.MapGroup("/api/users")
                                          .AddEndpointFilter<SessionAuthFilter>();

            // === [📥 GET ПОЛЬЗОВАТЕЛЯ ПО ID] ===
            protectedGroup.MapGet("/{id:guid}", async (Guid id) =>
            {
                var user = await userRepository.GetByIdAsync(id);
                if (user == null) return Results.NotFound(new { Error = "Пользователь не найден" });

                user.Password = string.Empty;
                return Results.Ok(user);
            });


            // === [🗑️ DELETE ПОЛЬЗОВАТЕЛЯ] ===
            protectedGroup.MapDelete("/{id:guid}", async (HttpContext httpContext, Guid id) =>
            {
                var session = (DeviceSyncEntity)httpContext.Items["CurrentSession"]!;

                // Удалять аккаунт может только сам владелец текущей активной сессии
                if (session.UserId != id)
                {
                    serverLogger.LogWarning($"[API DELETE] Попытка чужого удаления: сессия {session.UserId} пытается удалить {id}!");
                    return Results.Forbid();
                }

                await userRepository.DeleteByIdAsync(id);
                await deviceSyncRepository.DeleteSessionAsync(session.Id);
                serverLogger.LogInfo($"[API DELETE] Пользователь {id} успешно удалён.");
                return Results.Ok(new { Success = true });
            });




            // === [🚪 LOGOUT (АННУЛИРОВАНИЕ СЕССИИ)] ===
            protectedGroup.MapPost("/logout", async (HttpContext httpContext) =>
            {
                var session = (DeviceSyncEntity)httpContext.Items["CurrentSession"]!;
                await deviceSyncRepository.DeleteSessionAsync(session.Id);
                serverLogger.LogInfo($"[API LOGOUT] Сессия {session.Id} закрыта на сервере.");
                return Results.Ok(new { Success = true });
            });
        }

        public record LoginRequest(string Email, string Password, Guid SessionId = default);
    }
}
