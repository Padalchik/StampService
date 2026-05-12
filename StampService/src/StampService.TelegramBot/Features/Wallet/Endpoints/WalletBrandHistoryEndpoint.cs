using System.Globalization;
using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Application.Wallet.Queries.GetUserBrandWalletHistory;
using StampService.Contracts.DTOs.Users;
using StampService.Contracts.DTOs.Wallet;
using StampService.TelegramBot.Features.Wallet.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Wallet.Endpoints;

public sealed class WalletBrandHistoryEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapAction<ViewWalletBrandHistoryAction, ViewWalletBrandHistoryPayload>(HandleAsync);
    }

    private static async Task<IEndpointResult> HandleAsync(
        UpdateContext ctx,
        ViewWalletBrandHistoryPayload payload,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<UserBrandWalletHistoryResponse, GetUserBrandWalletHistoryQuery> historyHandler)
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

        var historyResult = await historyHandler.Handle(
            new GetUserBrandWalletHistoryQuery(
                userResult.Value.UserId,
                payload.BrandId,
                Skip: 0,
                Take: 10),
            ctx.CancellationToken);

        if (historyResult.IsFailed)
            return BotResults.ShowView(new ScreenView("Не удалось загрузить историю.").BackButton());

        var brandName = string.IsNullOrWhiteSpace(historyResult.Value.BrandName)
            ? payload.BrandName
            : historyResult.Value.BrandName;
        var title = $"<b>{Html(brandName)}</b>\nИстория кошелька";

        if (historyResult.Value.Items.Count == 0)
        {
            return BotResults.ShowView(new ScreenView(
                $"{title}\n\nИстории операций пока нет.")
                .BackButton());
        }

        var lines = historyResult.Value.Items.Select(item =>
        {
            var isIssue = item.TransactionType == "Issue";
            var marker = isIssue ? "🟢" : "🟡";
            var sign = isIssue ? "+" : "-";
            var date = item.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
            var comment = string.IsNullOrWhiteSpace(item.Comment) || IsAutoComment(item.Comment)
                ? string.Empty
                : $" - {Html(item.Comment)}";

            return $"{marker} {date}: {sign}{item.Amount} {Html(item.SourceName)}{comment}";
        });

        return BotResults.ShowView(new ScreenView(
            $"{title}\n\n" +
            "<b>Последние операции</b>\n" +
            string.Join("\n", lines))
            .BackButton());
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);

    private static bool IsAutoComment(string value)
    {
        return value is "Issue metric" or "Redeem metric" or "Issue coins" or "Redeem coins";
    }
}
