using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Wizards;

/// <summary>
/// Методы расширения для запуска визардов из обработчиков.
/// В большинстве случаев предпочтительнее возвращать
/// <c>BotResults.StartWizard&lt;TWizard&gt;()</c> из обработчика напрямую.
/// </summary>
public static class WizardContextExtensions
{
    /// <summary>
    /// Запускает визард из <see cref="BotExecutionContext"/>.
    /// Используй в реализациях <see cref="IEndpointResult.ExecuteAsync"/>, если нужно
    /// запустить визард императивно, а не через return-intent.
    /// </summary>
    public static Task StartWizardAsync<TWizard>(
        this BotExecutionContext ctx,
        CancellationToken cancellationToken = default)
        where TWizard : class, IBotWizard
        => BotResults.StartWizard<TWizard>().ExecuteAsync(ctx);

    /// <summary>
    /// Запускает визард из <see cref="UpdateContext"/>.
    /// Делегирует в перегрузку на <see cref="BotExecutionContext"/>.
    /// </summary>
    public static Task StartWizardAsync<TWizard>(
        this UpdateContext context,
        CancellationToken cancellationToken = default)
        where TWizard : class, IBotWizard
        => BotExecutionContext.FromUpdateContext(context).StartWizardAsync<TWizard>(cancellationToken);
}