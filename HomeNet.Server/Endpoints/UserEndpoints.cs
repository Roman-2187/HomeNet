using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Models.Shared.Entity;
using HomeNetOrm.Exeptions;
using HomeNetOrm.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace HomeNet.Server.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this IEndpointRouteBuilder endpoints, IUserRepo userRepository, ICastomLogger serverLogger)
        {
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

                    serverLogger.LogInfo($"[API AUTH] Успешный вход для {request.Email}");
                    return Results.Ok(user);
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API AUTH] Ошибка авторизации: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
            });

            // === [📥 GET ВСЕХ ПОЛЬЗОВАТЕЛЕЙ (ТОЛЬКО ДЛЯ АДМИНА)] ===
            endpoints.MapGet("/api/users", async (Guid userId, Guid deviceId) =>
            {
                try
                {
                    var user = await userRepository.GetByIdAsync(userId);
                    if (user == null) return Results.Unauthorized();

                    const string ADMIN_EMAIL = "ohagow.r@gmail.com";
                    if (user.Email != ADMIN_EMAIL || user.DeviceId != deviceId)
                    {
                        serverLogger.LogWarning($"[API] Доступ запрещён для {user.Email}.");
                        return Results.Unauthorized();
                    }

                    serverLogger.LogInfo($"[API] Запрос всех пользователей от админа {user.Email}.");
                    var users = await userRepository.GetAllAsync();
                    return Results.Ok(users);
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API Ошибка] Сбой получения всех пользователей: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
            });

            // === [📥 GET ПОЛЬЗОВАТЕЛЯ ПО ID] ===
            endpoints.MapGet("/api/users/{id:guid}", async (Guid id) =>
            {
                try
                {
                    serverLogger.LogInfo($"[API] Запрос пользователя по ID: {id}");
                    var user = await userRepository.GetByIdAsync(id);
                    return user == null
                        ? Results.NotFound(new { Error = "Пользователь не найден" })
                        : Results.Ok(user);
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API Ошибка] Сбой получения пользователя по ID: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
            });

            // === [📤 PUSH РЕГИСТРАЦИЯ И ОБНОВЛЕНИЕ ПРОФИЛЯ] ===
            endpoints.MapPost("/api/users", async (UserEntity incomingUser, IHubContext<ServerChatHub> hubContext) =>
            {
                try
                {
                    serverLogger.LogInfo($"[API PUSH] Прилетел профиль пользователя '{incomingUser.Email}' (ID: {incomingUser.Id})");
                    var existingServerUser = await userRepository.GetByIdAsync(incomingUser.Id);

                    if (existingServerUser != null)
                    {
                        existingServerUser.DeviceId = incomingUser.DeviceId;
                        existingServerUser.FirstName = incomingUser.FirstName;
                        existingServerUser.LastName = incomingUser.LastName;
                        existingServerUser.PhoneNumber = incomingUser.PhoneNumber;

                        await userRepository.UpdateAsync(existingServerUser);
                        serverLogger.LogInfo($"[API Успех] Профиль успешно обновлен.");
                    }
                    else
                    {
                        await userRepository.InsertAsync(incomingUser);
                        serverLogger.LogInfo($"[API Успех] Новый пользователь '{incomingUser.FirstName}' записан.");
                    }

                    return Results.Ok(new { Success = true });
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
                    serverLogger.LogError($"[API Ошибка] Не удалось обработать PUSH: {ex.Message}");
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

            // === [🗑️ DELETE ПОЛЬЗОВАТЕЛЯ] ===
            endpoints.MapDelete("/api/users/{id:guid}", async (Guid id) =>
            {
                try
                {
                    serverLogger.LogInfo($"[API DELETE] Запрос на удаление пользователя: {id}");
                    var user = await userRepository.GetByIdAsync(id);
                    if (user == null)
                        return Results.NotFound(new { Error = "Пользователь не найден" });

                    await userRepository.DeleteByIdAsync(id);
                    serverLogger.LogInfo($"[API DELETE] Пользователь {id} успешно удалён.");
                    return Results.Ok(new { Success = true });
                }
                catch (Exception ex)
                {
                    serverLogger.LogError($"[API DELETE] Ошибка удаления: {ex.Message}");
                    return Results.Problem("Ошибка сервера");
                }
            });
        }

        public record LoginRequest(string Email, string Password);
    }
}
