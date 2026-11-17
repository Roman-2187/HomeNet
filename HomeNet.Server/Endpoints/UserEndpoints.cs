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
                    serverLogger.LogInfo($"[API AUTH] Попытка входа для {request.Email}...");
                    var user = await userRepository.GetByEmailAsync(request.Email);

                    if (user == null || user.Password != request.Password)
                    {
                        serverLogger.LogWarning($"[API AUTH] Неверный email или пароль для {request.Email}");
                        return Results.Unauthorized();
                    }

                    // 1. Генерируем токен сессии (или берем присланный, если клиент его передал)
                    Guid sessionId = request.SessionId != Guid.Empty
                        ? request.SessionId
                        : Guid.NewGuid();

                    // 2. Фиксируем или продлеваем сессию в таблице device_sync на сервере
                    await deviceSyncRepository.UpsertSessionAsync(sessionId, user.Id);

                    // 3. Возвращаем пользователя с активным токеном сессии, но без пароля
                    user.SessionId = sessionId;
                    user.Password = string.Empty;

                    serverLogger.LogInfo($"[API AUTH] Успешный вход для {request.Email} (SessionId: {sessionId})");
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
                    serverLogger.LogInfo($"[API PUSH] Регистрация пользователя '{incomingUser.Email}' (ID: {incomingUser.Id})");

                    // 1. Записываем нового пользователя в PostgreSQL
                    await userRepository.InsertAsync(incomingUser);

                    // 2. Если клиент не прислал SessionId, генерируем его на сервере; иначе берем присланный
                    Guid sessionId = incomingUser.SessionId != Guid.Empty
                        ? incomingUser.SessionId
                        : Guid.NewGuid();

                    // 3. Открываем сессию в таблице device_sync
                    await deviceSyncRepository.UpsertSessionAsync(sessionId, incomingUser.Id);

                    // 4. Подготавливаем безопасный ответ (с токеном сессии, но без пароля)
                    incomingUser.SessionId = sessionId;
                    incomingUser.Password = string.Empty;

                    serverLogger.LogInfo($"[API Успех] Новый пользователь '{incomingUser.FirstName}' записан. SessionId: {sessionId}");
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
                var caller = await userRepository.GetByIdAsync(session.UserId);

                const string ADMIN_EMAIL = "ohagow.r@gmail.com";
                // Удалять может либо сам владелец аккаунта, либо админ
                if (session.UserId != id && caller?.Email != ADMIN_EMAIL)
                {
                    serverLogger.LogWarning($"[API DELETE] Попытка чужого удаления от {session.UserId}!");
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
