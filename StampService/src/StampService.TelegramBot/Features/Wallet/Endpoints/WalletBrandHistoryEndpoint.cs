using System.Globalization;
using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Application.Wallet.Queries.GetUserBrandWalletHistory;
using StampService.Application.Wallet.Queries.GetUserBrandRewards;
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
        app.MapAction<ViewWalletBrandRewardsAction, ViewWalletBrandRewardsPayload>(ViewRewardsAsync);
        app.MapAction<ViewWalletBrandHistoryAction, ViewWalletBrandHistoryPayload>(HandleAsync);
    }

    private static async Task<IEndpointResult> ViewRewardsAsync(
        UpdateContext ctx,
        ViewWalletBrandRewardsPayload payload,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<UserBrandRewardsResponse, GetUserBrandRewardsQuery> rewardsHandler)
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

        var rewardsResult = await rewardsHandler.Handle(
            new GetUserBrandRewardsQuery(userResult.Value.UserId, payload.BrandId),
            ctx.CancellationToken);

        if (rewardsResult.IsFailed)
            return BotResults.ShowView(new ScreenView("Не удалось загрузить бренд.").BackButton());

        var brandName = string.IsNullOrWhiteSpace(rewardsResult.Value.BrandName)
            ? payload.BrandName
            : rewardsResult.Value.BrandName;

        return BotResults.ShowView(new ScreenView(BuildRewardsText(rewardsResult.Value, brandName))
            .Button<ViewWalletBrandHistoryAction, ViewWalletBrandHistoryPayload>(
                "📈 История",
                new ViewWalletBrandHistoryPayload(payload.BrandId, brandName))
            .BackButton());
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

    private static string BuildRewardsText(UserBrandRewardsResponse response, string brandName)
    {
        var sections = new List<string>
        {
            $"<b>{Html(brandName)}</b>",
            $"Монетки: {response.CoinBalance}"
        };

        sections.Add(BuildProductsText(response));
        sections.Add(BuildMetricsText(response));
        sections.Add("Чтобы получить награду, покажите код для списания сотруднику.");

        return string.Join("\n\n", sections);
    }

    private static string BuildProductsText(UserBrandRewardsResponse response)
    {
        if (response.CoinProducts.Count == 0)
            return "<b>Товары за монетки</b>\nПока нет активных товаров.";

        var lines = response.CoinProducts.Select(product =>
        {
            var status = product.IsAvailable
                ? "доступно"
                : $"не хватает {product.MissingAmount}";

            var marker = product.IsAvailable ? "✅" : "▫️";
            return $"{marker} {Html(product.ProductName)} · {product.CurrentBalance}/{product.Price} · {status}";
        });

        return "<b>Товары за монетки</b>\n" + string.Join("\n", lines);
    }

    private static string BuildMetricsText(UserBrandRewardsResponse response)
    {
        if (response.Metrics.Count == 0)
            return "<b>Метрики</b>\nПока нет балансов по метрикам.";

        var lines = response.Metrics.Select(metric =>
        {
            var status = metric.IsAvailable
                ? "доступно"
                : $"не хватает {metric.MissingAmount}";

            var marker = metric.IsAvailable ? "✅" : "▫️";
            return $"{marker} {Html(metric.MetricName)} · {metric.CurrentBalance}/{metric.RequiredAmount} · {status}";
        });

        return "<b>Метрики</b>\n" + string.Join("\n", lines);
    }

    private static bool IsAutoComment(string value)
    {
        return value is "Issue metric" or "Redeem metric" or "Issue coins" or "Redeem coins";
    }
}
