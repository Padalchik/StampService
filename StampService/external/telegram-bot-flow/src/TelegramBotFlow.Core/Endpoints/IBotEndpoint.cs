using TelegramBotFlow.Core.Hosting;

namespace TelegramBotFlow.Core.Endpoints;

public interface IBotEndpoint
{
    void MapEndpoint(BotApplication app);
}