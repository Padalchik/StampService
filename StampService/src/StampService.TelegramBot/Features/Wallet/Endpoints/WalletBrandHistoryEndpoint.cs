using System.Globalization;
using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Application.Wallet.Queries.GetUserWalletBrandDetails;
using StampService.Contracts.DTOs.Users;
using StampService.Contracts.DTOs.Wallet;
using StampService.TelegramBot.Common.Routing;
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
        IQueryHandler<UserWalletBrandDetailsResponse, GetUserWalletBrandDetailsQuery> detailsHandler)
    {
        var userResult = await BotEndpointHelpers.EnsureUserAsync(ctx, ensureUserHandler);

        if (userResult.IsFailed)
            return BotResults.ShowView(new ScreenView("Не удалось определить пользователя.").BackButton());

        var detailsResult = await detailsHandler.Handle(
            new GetUserWalletBrandDetailsQuery(userResult.Value.UserId, payload.BrandId),
            ctx.CancellationToken);

        if (detailsResult.IsFailed)
            return BotResults.ShowView(new ScreenView("Не удалось загрузить бренд.").BackButton());

        var brandName = string.IsNullOrWhiteSpace(detailsResult.Value.BrandName)
            ? payload.BrandName
            : detailsResult.Value.BrandName;

        return BotResults.ShowView(new ScreenView(BuildRewardsText(detailsResult.Value, brandName))
            .Button<ViewWalletBrandHistoryAction, ViewWalletBrandHistoryPayload>(
                "📈 История",
                new ViewWalletBrandHistoryPayload(payload.BrandId, brandName))
            .BackButton());
    }

    private static async Task<IEndpointResult> HandleAsync(
        UpdateContext ctx,
        ViewWalletBrandHistoryPayload payload,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<UserWalletBrandDetailsResponse, GetUserWalletBrandDetailsQuery> detailsHandler)
    {
        var userResult = await BotEndpointHelpers.EnsureUserAsync(ctx, ensureUserHandler);

        if (userResult.IsFailed)
            return BotResults.ShowView(new ScreenView("Не удалось определить пользователя.").BackButton());

        var detailsResult = await detailsHandler.Handle(
            new GetUserWalletBrandDetailsQuery(
                userResult.Value.UserId,
                payload.BrandId),
            ctx.CancellationToken);

        if (detailsResult.IsFailed)
            return BotResults.ShowView(new ScreenView("Не удалось загрузить историю.").BackButton());

        var brandName = string.IsNullOrWhiteSpace(detailsResult.Value.BrandName)
            ? payload.BrandName
            : detailsResult.Value.BrandName;
        var title = $"<b>{Html(brandName)}</b>\nИстория кошелька";

        var history = detailsResult.Value.History;
        var hasItems = history.Groups.Any(group => group.Items.Count > 0);
        if (!hasItems)
        {
            return BotResults.ShowView(new ScreenView(
                $"{title}\n\n{Html(history.EmptyText)}")
                .BackButton());
        }

        var sections = BuildHistorySections(history);

        return BotResults.ShowView(new ScreenView(
            $"{title}\n\n" +
            $"<b>{Html(history.Title)}</b>\n" +
            string.Join("\n\n", sections))
            .BackButton());
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);

    private static IReadOnlyCollection<string> BuildHistorySections(UserWalletBrandHistorySectionResponse response)
    {
        return response.Groups
            .Select(BuildHistorySection)
            .ToArray();
    }

    private static string BuildHistorySection(UserWalletBrandHistoryGroupResponse group)
    {
        var lines = group.Items
            .Select(FormatHistoryItem)
            .ToArray();

        return lines.Length == 0
            ? $"<b>{Html(group.Title)}</b>\n{Html(group.EmptyText)}"
            : $"<b>{Html(group.Title)}</b>\n" + string.Join("\n", lines);
    }

    private static string FormatHistoryItem(UserWalletBrandHistoryItemDetailsResponse item)
    {
        var isIssue = item.TransactionType == "Issue";
        var marker = isIssue ? "🟢" : "🟡";
        var date = item.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        var comment = item.HasVisibleComment && !string.IsNullOrWhiteSpace(item.Comment)
            ? $" - {Html(item.Comment!)}"
            : string.Empty;

        return $"{marker} {date}: {Html(item.AmountText)}{comment}";
    }

    private static string BuildRewardsText(UserWalletBrandDetailsResponse response, string brandName)
    {
        var sections = new List<string> { $"<b>{Html(brandName)}</b>" };

        foreach (var rewardSection in response.RewardSections)
            sections.Add(BuildRewardSectionText(rewardSection));

        sections.Add(response.HintText);

        return string.Join("\n\n", sections);
    }

    private static string BuildRewardSectionText(UserWalletBrandRewardSectionResponse section)
    {
        var lines = new List<string> { $"<b>{Html(section.Title)}</b>" };

        if (!string.IsNullOrWhiteSpace(section.BalanceText))
            lines.Add(Html(section.BalanceText));

        if (section.Items.Count == 0)
        {
            lines.Add(Html(section.EmptyText));
            return string.Join("\n", lines);
        }

        lines.AddRange(section.Items.Select(item =>
        {
            var marker = item.IsAvailable ? "✅" : "▫️";
            return $"{marker} {Html(item.Name)} · {Html(item.ProgressText)} · {Html(item.StatusText)}";
        }));

        return string.Join("\n", lines);
    }
}
