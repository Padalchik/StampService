using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.Brands.Queries.GetBrandWorkspace;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Brands;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Brands.Screens;

public sealed class BrandWorkspaceScreen : IScreen
{
    public const string BrandIdSessionKey = "brand_workspace.brand_id";

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
        var view = new ScreenView(
            $"<b>{Html(workspace.BrandName)}</b>\n" +
            $"Роль: {Html(workspace.RoleSystemName)}\n\n" +
            "Доступные действия:");

        var hasActions = false;

        if (workspace.CanIssue)
        {
            view.Row().Button("Выдать метрику", "brand_issue_not_implemented");
            hasActions = true;
        }

        if (workspace.CanRedeem)
        {
            view.Row().Button("Списать метрику", "brand_redeem_not_implemented");
            hasActions = true;
        }

        if (workspace.CanViewBalances)
        {
            view.Row().Button("Балансы клиентов", "brand_balances_not_implemented");
            hasActions = true;
        }

        if (workspace.CanManageMetrics)
        {
            view.Row().Button("Метрики", "brand_metrics_not_implemented");
            hasActions = true;
        }

        if (workspace.CanManageStaff)
        {
            view.Row().Button("Сотрудники", "brand_staff_not_implemented");
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
