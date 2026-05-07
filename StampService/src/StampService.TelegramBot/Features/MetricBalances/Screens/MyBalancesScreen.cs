using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.Metrics.Queries.GetUserMetricBalances;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Metrics;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.MetricBalances.Screens;

public sealed class MyBalancesScreen : IScreen
{
    private readonly ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> _ensureUserHandler;
    private readonly IQueryHandler<UserMetricBalancesResponse, GetUserMetricBalancesQuery> _balancesHandler;

    public MyBalancesScreen(
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<UserMetricBalancesResponse, GetUserMetricBalancesQuery> balancesHandler)
    {
        _ensureUserHandler = ensureUserHandler;
        _balancesHandler = balancesHandler;
    }

    public async ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
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

        var balancesResult = await _balancesHandler.Handle(
            new GetUserMetricBalancesQuery(userResult.Value.UserId),
            ctx.CancellationToken);

        if (balancesResult.IsFailed)
            return new ScreenView("Не удалось загрузить балансы.").BackButton();

        if (balancesResult.Value.Balances.Count == 0)
        {
            return new ScreenView(
                "<b>Мои балансы</b>\n\n" +
                "У вас пока нет балансов по метрикам.")
                .BackButton();
        }

        var lines = balancesResult.Value.Balances
            .Select(balance =>
                $"• <b>{Html(balance.BrandName)}</b>: {Html(balance.MetricName)} - {balance.Value}");

        return new ScreenView(
            "<b>Мои балансы</b>\n\n" +
            string.Join("\n", lines))
            .BackButton();
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
