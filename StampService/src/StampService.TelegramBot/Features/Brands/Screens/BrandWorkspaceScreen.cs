using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.Brands.Queries.GetBrandWorkspace;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Brands;
using StampService.TelegramBot.Features.CustomerBalances.Actions;
using StampService.TelegramBot.Features.IssueMetric.Screens;
using StampService.TelegramBot.Features.Metrics.Screens;
using StampService.TelegramBot.Features.RedeemMetric.Screens;
using StampService.TelegramBot.Features.Staff.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Brands.Screens;

public sealed class BrandWorkspaceScreen : IScreen
{
    public const string BrandIdSessionKey = "brand_workspace.brand_id";
    public const string BrandNameSessionKey = "brand_workspace.brand_name";

    private readonly ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> _ensureUserHandler;
    private readonly IQueryHandler<BrandWorkspaceResponse, GetBrandWorkspaceQuery> _workspaceHandler;

    public BrandWorkspaceScreen(
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<BrandWorkspaceResponse, GetBrandWorkspaceQuery> workspaceHandler)
    {
        _ensureUserHandler = ensureUserHandler;
        _workspaceHandler = workspaceHandler;
    }

    public async ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        if (ctx.Session?.Data.Get<Guid>(BrandIdSessionKey) is not { } brandId || brandId == Guid.Empty)
            return new ScreenView("Бренд не выбран.").BackButton();

        var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
        var userResult = await _ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(
                ctx.UserId,
                from?.FirstName,
                from?.LastName,
                from?.Username),
            ctx.CancellationToken);

        if (userResult.IsFailed)
            return new ScreenView("Не удалось определить пользователя.").BackButton();

        var workspaceResult = await _workspaceHandler.Handle(
            new GetBrandWorkspaceQuery(userResult.Value.UserId, brandId),
            ctx.CancellationToken);

        if (workspaceResult.IsFailed)
            return new ScreenView("Не удалось открыть бренд.").BackButton();

        var workspace = workspaceResult.Value;
        ctx.Session?.Data.Set(BrandNameSessionKey, workspace.BrandName);

        var view = new ScreenView(
            $"<b>{Html(workspace.BrandName)}</b>\n" +
            $"Роль: {Html(workspace.RoleSystemName)}\n\n" +
            "Доступные действия:");

        var hasActions = false;

        if (workspace.CanIssue)
        {
            view.Row().NavigateButton<IssueMetricSelectScreen>("Выдать метрику");
            hasActions = true;
        }

        if (workspace.CanRedeem)
        {
            view.Row().NavigateButton<RedeemMetricCodeScreen>("Списать метрику");
            hasActions = true;
        }

        if (workspace.CanViewBalances)
        {
            view.Row().Button<StartCustomerBalancesAction>("Балансы клиентов");
            hasActions = true;
        }

        if (workspace.CanManageMetrics)
        {
            view.Row().NavigateButton<MetricsListScreen>("Метрики");
            hasActions = true;
        }

        if (workspace.CanManageStaff)
        {
            view.Row().Button<OpenBrandStaffAction, OpenBrandStaffPayload>(
                "Сотрудники",
                new OpenBrandStaffPayload(workspace.BrandId, workspace.BrandName));
            hasActions = true;
        }

        if (!hasActions)
            view = new ScreenView($"<b>{Html(workspace.BrandName)}</b>\n\nНет доступных действий.");

        return view.BackButton();
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
