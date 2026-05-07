using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Routing;

namespace StampService.TelegramBot.Common.Routing;

public sealed record DeleteIncomingMessageResult(IEndpointResult Inner) : IEndpointResult
{
    public async Task ExecuteAsync(BotExecutionContext context)
    {
        if (context.Update.Update.Message is not null)
        {
            try
            {
                await context.Responder.DeleteMessageAsync(context.Update);
            }
            catch
            {
                // Telegram may reject deletion for old or already deleted messages.
                // The main scenario should continue even if cleanup fails.
            }
        }

        await Inner.ExecuteAsync(context);
    }
}
