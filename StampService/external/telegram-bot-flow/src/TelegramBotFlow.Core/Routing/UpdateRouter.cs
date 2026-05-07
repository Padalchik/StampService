using Microsoft.Extensions.Logging;
using TelegramBotFlow.Core.Pipeline;

namespace TelegramBotFlow.Core.Routing;

/// <summary>
/// Сопоставляет входящий update с первым подходящим маршрутом и вызывает его обработчик.
/// </summary>
internal sealed class UpdateRouter
{
    private readonly List<RouteEntry> _routes = [];
    private readonly ILogger<UpdateRouter> _logger;
    private UpdateDelegate? _fallbackHandler;

    public UpdateRouter(ILogger<UpdateRouter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Добавляет маршрут в таблицу маршрутизации.
    /// </summary>
    /// <param name="route">Маршрут для регистрации.</param>
    public void AddRoute(RouteEntry route) => _routes.Add(route);

    /// <summary>
    /// Устанавливает fallback-обработчик при отсутствии совпавшего маршрута.
    /// </summary>
    /// <param name="handler">Fallback-делегат.</param>
    public void SetFallback(UpdateDelegate handler) => _fallbackHandler = handler;

    /// <summary>
    /// Строит терминальный делегат маршрутизации с сортировкой по приоритетам.
    /// </summary>
    /// <returns>Делегат, обрабатывающий update по зарегистрированным маршрутам.</returns>
    public UpdateDelegate BuildTerminal()
    {
        List<RouteEntry> sorted = _routes
            .OrderBy(r => r.Priority)
            .ToList();

        return async context =>
        {
            foreach (RouteEntry route in sorted)
            {
                if (!route.Matches(context))
                    continue;

                _logger.LogDebug(
                    "Matched route {RouteType} {Pattern} for user {UserId}",
                    route.Type,
                    route.Pattern ?? "(predicate)",
                    context.UserId);

                context.HandlerName = route.Pattern ?? route.Type.ToString();
                await route.Handler(context);
                return;
            }

            if (_fallbackHandler is not null)
            {
                _logger.LogDebug(
                    "No route matched, invoking fallback for user {UserId}",
                    context.UserId);

                await _fallbackHandler(context);
                return;
            }

            _logger.LogDebug(
                "No route matched for update {UpdateType} from user {UserId}",
                context.UpdateType,
                context.UserId);
        };
    }
}