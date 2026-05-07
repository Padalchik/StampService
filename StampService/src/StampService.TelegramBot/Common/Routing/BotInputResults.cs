using TelegramBotFlow.Core.Routing;

namespace StampService.TelegramBot.Common.Routing;

public static class BotInputResults
{
    public static IEndpointResult DeleteInputThen(IEndpointResult inner)
    {
        return new DeleteIncomingMessageResult(inner);
    }
}
