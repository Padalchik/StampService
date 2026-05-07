using StampService.TelegramBot.Features.Brands.Actions;
using StampService.TelegramBot.Features.Brands.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;

namespace StampService.TelegramBot.Features.Brands.Endpoints;

public sealed class BrandWorkspaceEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapAction<OpenBrandWorkspaceAction, OpenBrandWorkspacePayload>((
            UpdateContext ctx,
            OpenBrandWorkspacePayload payload) =>
        {
            ctx.Session?.Data.Set(BrandWorkspaceScreen.BrandIdSessionKey, payload.BrandId);
            return Task.FromResult(BotResults.NavigateTo<BrandWorkspaceScreen>());
        });
    }
}
