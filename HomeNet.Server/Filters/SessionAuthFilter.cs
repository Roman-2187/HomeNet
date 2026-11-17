   
    using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
    using HomeNetOrm.Repositories.Remote;

    namespace HomeNet.Server.Filters
    {
        public class SessionAuthFilter(DeviceSyncRepository deviceSyncRepository, ICastomLogger logger) : IEndpointFilter
        {
            public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
            {
                var httpContext = context.HttpContext;

                if (!httpContext.Request.Headers.TryGetValue("X-Session-Id", out var sessionHeader) ||
                    !Guid.TryParse(sessionHeader, out var sessionId) ||
                    sessionId == Guid.Empty)
                {
                    logger.LogWarning($"[AUTH FILTER] Отклонён запрос без X-Session-Id: {httpContext.Request.Path}");
                    return Results.Unauthorized();
                }

                // Проверяем наличие этого SessionId в таблице device_sync на сервере
                var session = await deviceSyncRepository.GetByIdAsync(sessionId);
                if (session == null)
                {
                    logger.LogWarning($"[AUTH FILTER] Неизвестная или закрытая сессия {sessionId}: {httpContext.Request.Path}");
                    return Results.Unauthorized();
                }

                // Кладём проверенную сессию в контекст запроса
                httpContext.Items["CurrentSession"] = session;

                return await next(context);
            }
        }
    }


