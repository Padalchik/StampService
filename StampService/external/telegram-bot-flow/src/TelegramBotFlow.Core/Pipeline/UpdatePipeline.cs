using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Pipeline;

/// <summary>
/// Выполняет цепочку middleware и терминальный обработчик для одного update-а.
/// </summary>
internal sealed class UpdatePipeline
{
    private readonly UpdateDelegate _pipeline;

    private UpdatePipeline(UpdateDelegate pipeline)
    {
        _pipeline = pipeline;
    }

    /// <summary>
    /// Запускает обработку update-а через собранный pipeline.
    /// </summary>
    /// <param name="context">Контекст update-а.</param>
    /// <returns>Задача обработки update-а.</returns>
    public Task ProcessAsync(UpdateContext context) => _pipeline(context);

    /// <summary>
    /// Собирает pipeline из списка middleware и терминального обработчика.
    /// </summary>
    /// <param name="middlewares">Middleware-фабрики, оборачивающие следующий делегат.</param>
    /// <param name="terminal">Финальный обработчик после middleware.</param>
    /// <returns>Готовый к выполнению экземпляр pipeline.</returns>
    public static UpdatePipeline Build(
        IReadOnlyList<Func<UpdateDelegate, UpdateDelegate>> middlewares,
        UpdateDelegate terminal)
    {
        UpdateDelegate pipeline = terminal;

        for (int i = middlewares.Count - 1; i >= 0; i--)
        {
            pipeline = middlewares[i](pipeline);
        }

        return new UpdatePipeline(pipeline);
    }
}