using System.Globalization;
using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.Metrics.Queries.GetUserMetricTransactions;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Metrics;
using StampService.TelegramBot.Features.MetricBalances.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.MetricBalances.Endpoints;

public sealed class MetricBalanceHistoryEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapAction<ViewBalanceHistoryAction, ViewBalanceHistoryPayload>(HandleAsync);
    }

    private static async Task<IEndpointResult> HandleAsync(
        UpdateContext ctx,
        ViewBalanceHistoryPayload payload,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<MetricTransactionsResponse, GetUserMetricTransactionsQuery> transactionsHandler)
    {
        var from = ctx.Update.CallbackQuery?.From ?? ctx.Update.Message?.From;
        var userResult = await ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(
                ctx.UserId,
                from?.FirstName,
                from?.LastName,
                from?.Username),
            ctx.CancellationToken);

        if (userResult.IsFailed)
            return BotResults.ShowView(new ScreenView("Не удалось определить пользователя.").BackButton());

        var transactionsResult = await transactionsHandler.Handle(
            new GetUserMetricTransactionsQuery(
                payload.MetricDefinitionId,
                userResult.Value.UserId,
                Skip: 0,
                Take: 10),
            ctx.CancellationToken);

        if (transactionsResult.IsFailed)
            return BotResults.ShowView(new ScreenView("Не удалось загрузить историю.").BackButton());

        var title = $"<b>{Html(payload.BrandName)}</b>\n{Html(payload.MetricName)}";
        if (transactionsResult.Value.Items.Count == 0)
        {
            return BotResults.ShowView(new ScreenView(
                $"{title}\n\n" +
                "Истории операций пока нет.")
                .BackButton());
        }

        var lines = transactionsResult.Value.Items.Select(transaction =>
        {
            var isIssue = transaction.TransactionType == "Issue";
            var marker = isIssue ? "🟢" : "🟡";
            var sign = isIssue ? "+" : "-";
            var date = transaction.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
            var comment = string.IsNullOrWhiteSpace(transaction.Comment)
                ? string.Empty
                : $" - {Html(transaction.Comment)}";

            return $"{marker} {date}: {sign}{transaction.Amount} {Html(payload.MetricName)}{comment}";
        });

        return BotResults.ShowView(new ScreenView(
            $"{title}\n\n" +
            "<b>Последние операции</b>\n" +
            string.Join("\n", lines))
            .BackButton());
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
