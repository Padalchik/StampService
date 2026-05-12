using StampService.TelegramBot.Features.Wallet.Actions;
using StampService.TelegramBot.Features.Wallet.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;

namespace StampService.TelegramBot.Features.Wallet.Endpoints;

public sealed class MyWalletEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapAction<RefreshMyWalletAction>(RefreshAsync);
    }

    private static Task<IEndpointResult> RefreshAsync(UpdateContext ctx)
    {
        ctx.Session?.Data.Set(MyWalletScreen.ForceRefreshCodeSessionKey, true);
        return Task.FromResult(BotResults.NavigateTo<MyWalletScreen>());
    }
}
