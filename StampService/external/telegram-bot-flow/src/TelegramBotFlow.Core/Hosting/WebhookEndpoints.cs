using Telegram.Bot.Types;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Pipeline;

namespace TelegramBotFlow.Core.Hosting;

internal static class WebhookEndpoints
{
    public static async Task HandleWebhookUpdate(
        Update update,
        UpdatePipeline pipeline,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var context = new UpdateContext(update, services, cancellationToken);
        await pipeline.ProcessAsync(context);
    }
}