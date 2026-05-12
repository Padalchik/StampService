using StampService.Application.Abstractions;
using StampService.Application.Brands.Queries.GetBrandWorkspace;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Brands;
using StampService.TelegramBot.Features.Brands.Actions;
using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.Metrics.Screens;
using StampService.TelegramBot.Features.Staff.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;

namespace StampService.TelegramBot.Features.Brands.Endpoints;

public sealed class BrandWorkspaceEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapAction<OpenBrandWorkspaceAction, OpenBrandWorkspacePayload>(async (
            UpdateContext ctx,
            OpenBrandWorkspacePayload payload,
            ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
            IQueryHandler<BrandWorkspaceResponse, GetBrandWorkspaceQuery> workspaceHandler) =>
        {
            ctx.Session?.Data.Set(BrandWorkspaceScreen.BrandIdSessionKey, payload.BrandId);

            var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
            var userResult = await ensureUserHandler.Handle(
                new EnsureTelegramUserCommand(
                    ctx.UserId,
                    from?.FirstName,
                    from?.LastName,
                    from?.Username),
                ctx.CancellationToken);

            if (userResult.IsFailed)
                return BotResults.NavigateTo<BrandWorkspaceScreen>();

            var workspaceResult = await workspaceHandler.Handle(
                new GetBrandWorkspaceQuery(userResult.Value.UserId, payload.BrandId),
                ctx.CancellationToken);

            if (workspaceResult.IsFailed)
                return BotResults.NavigateTo<BrandWorkspaceScreen>();

            var workspace = workspaceResult.Value;
            StoreWorkspace(ctx, workspace);

            var directSection = GetSingleAvailableSection(workspace);
            return directSection?.Invoke() ?? BotResults.NavigateTo<BrandWorkspaceScreen>();
        });
    }

    private static Func<IEndpointResult>? GetSingleAvailableSection(BrandWorkspaceResponse workspace)
    {
        var sections = new List<Func<IEndpointResult>>();

        if (workspace.CanIssue || workspace.CanRedeem || workspace.CanViewBalances)
            sections.Add(() => BotResults.NavigateTo<ClientWorkScreen>());

        if (workspace.CanManageMetrics)
            sections.Add(() => BotResults.NavigateTo<MetricsListScreen>());

        if (workspace.CanManageStaff)
            sections.Add(() => BotResults.NavigateTo<BrandStaffListScreen>());

        return sections.Count == 1 ? sections[0] : null;
    }

    private static void StoreWorkspace(UpdateContext ctx, BrandWorkspaceResponse workspace)
    {
        ctx.Session?.Data.Set(BrandWorkspaceScreen.BrandNameSessionKey, workspace.BrandName);
        ctx.Session?.Data.Set(BrandWorkspaceScreen.CanIssueSessionKey, workspace.CanIssue);
        ctx.Session?.Data.Set(BrandWorkspaceScreen.CanRedeemSessionKey, workspace.CanRedeem);
        ctx.Session?.Data.Set(BrandWorkspaceScreen.CanViewBalancesSessionKey, workspace.CanViewBalances);
        ctx.Session?.Data.Set(BrandWorkspaceScreen.CanManageMetricsSessionKey, workspace.CanManageMetrics);
        ctx.Session?.Data.Set(BrandWorkspaceScreen.CanManageStaffSessionKey, workspace.CanManageStaff);
    }
}
