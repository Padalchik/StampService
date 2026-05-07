using TelegramBotFlow.Core.Pipeline;

namespace TelegramBotFlow.Core.Routing;

/// <summary>
/// Реестр обработчиков пользовательского ввода по идентификатору действия.
/// </summary>
internal sealed class InputHandlerRegistry
{
    private readonly Dictionary<string, UpdateDelegate> _handlers = [];

    /// <summary>
    /// Регистрирует обработчик для указанного action-id.
    /// </summary>
    /// <param name="actionId">Идентификатор ожидаемого ввода.</param>
    /// <param name="handler">Делегат обработки ввода.</param>
    public void Register(string actionId, UpdateDelegate handler) =>
        _handlers[actionId] = handler;

    /// <summary>
    /// Возвращает обработчик для action-id, если он зарегистрирован.
    /// </summary>
    /// <param name="actionId">Идентификатор ожидаемого ввода.</param>
    /// <returns>Обработчик ввода или <see langword="null"/>.</returns>
    public UpdateDelegate? Find(string actionId) =>
        _handlers.GetValueOrDefault(actionId);
}