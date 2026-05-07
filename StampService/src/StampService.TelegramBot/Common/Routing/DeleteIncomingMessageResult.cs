using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Routing;

namespace StampService.TelegramBot.Common.Routing;

public sealed record DeleteIncomingMessageResult(IEndpointResult Inner) : IEndpointResult
{
    public async Task ExecuteAsync(BotExecutionContext context)
    {
        var incomingMessageId = context.Update.Update.Message?.Id;
        if (incomingMessageId is not null)
        {
            try
            {
                await context.Responder.DeleteMessageAsync(context.Update, incomingMessageId.Value);
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
