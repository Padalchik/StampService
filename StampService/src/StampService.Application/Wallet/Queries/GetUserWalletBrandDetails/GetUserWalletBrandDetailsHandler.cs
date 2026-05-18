using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Wallet.Queries.GetUserBrandRewards;
using StampService.Application.Wallet.Queries.GetUserBrandWalletHistory;
using StampService.Contracts.DTOs.Wallet;

namespace StampService.Application.Wallet.Queries.GetUserWalletBrandDetails;

public class GetUserWalletBrandDetailsHandler
    : IQueryHandler<UserWalletBrandDetailsResponse, GetUserWalletBrandDetailsQuery>
{
    private const int HistoryTake = 10;

    private readonly IQueryHandler<UserBrandRewardsResponse, GetUserBrandRewardsQuery> _rewardsHandler;
    private readonly IQueryHandler<UserBrandWalletHistoryResponse, GetUserBrandWalletHistoryQuery> _historyHandler;

    public GetUserWalletBrandDetailsHandler(
        IQueryHandler<UserBrandRewardsResponse, GetUserBrandRewardsQuery> rewardsHandler,
        IQueryHandler<UserBrandWalletHistoryResponse, GetUserBrandWalletHistoryQuery> historyHandler)
    {
        _rewardsHandler = rewardsHandler;
        _historyHandler = historyHandler;
    }

    public async Task<Result<UserWalletBrandDetailsResponse>> Handle(
        GetUserWalletBrandDetailsQuery query,
        CancellationToken cancellationToken)
    {
        var rewardsResult = await _rewardsHandler.Handle(
            new GetUserBrandRewardsQuery(query.UserId, query.BrandId),
            cancellationToken);

        if (rewardsResult.IsFailed)
            return Result.Fail<UserWalletBrandDetailsResponse>(rewardsResult.Errors);

        var historyResult = await _historyHandler.Handle(
            new GetUserBrandWalletHistoryQuery(query.UserId, query.BrandId, Skip: 0, Take: HistoryTake),
            cancellationToken);

        if (historyResult.IsFailed)
            return Result.Fail<UserWalletBrandDetailsResponse>(historyResult.Errors);

        var rewards = rewardsResult.Value;
        var history = historyResult.Value;
        var brandName = string.IsNullOrWhiteSpace(rewards.BrandName)
            ? history.BrandName
            : rewards.BrandName;

        return Result.Ok(new UserWalletBrandDetailsResponse(
            query.UserId,
            query.BrandId,
            brandName,
            BuildRewardSections(rewards),
            BuildHistorySection(history),
            "Чтобы получить награду, покажите код для списания сотруднику."));
    }

    private static IReadOnlyCollection<UserWalletBrandRewardSectionResponse> BuildRewardSections(
        UserBrandRewardsResponse response)
    {
        var sections = new List<UserWalletBrandRewardSectionResponse>();

        if (response.IsCoinsEnabled)
        {
            var items = response.IsCoinProductRedemptionEnabled
                ? response.CoinProducts
                    .Select(product => new UserWalletBrandRewardItemResponse(
                        product.ProductId,
                        product.ProductName,
                        $"{product.CurrentBalance}/{product.Price}",
                        product.IsAvailable ? "доступно" : $"не хватает {product.MissingAmount}",
                        product.IsAvailable))
                    .ToArray()
                : Array.Empty<UserWalletBrandRewardItemResponse>();

            sections.Add(new UserWalletBrandRewardSectionResponse(
                "CoinProducts",
                "Товары за монетки",
                $"Монетки: {response.CoinBalance}",
                "Пока нет активных товаров.",
                items));
        }

        if (response.IsMetricsEnabled)
        {
            sections.Add(new UserWalletBrandRewardSectionResponse(
                "Metrics",
                "Метрики",
                null,
                "Пока нет балансов по метрикам.",
                response.Metrics
                    .Select(metric => new UserWalletBrandRewardItemResponse(
                        metric.MetricDefinitionId,
                        metric.MetricName,
                        $"{metric.CurrentBalance}/{metric.RequiredAmount}",
                        metric.IsAvailable ? "доступно" : $"не хватает {metric.MissingAmount}",
                        metric.IsAvailable))
                    .ToArray()));
        }

        return sections;
    }

    private static UserWalletBrandHistorySectionResponse BuildHistorySection(
        UserBrandWalletHistoryResponse response)
    {
        var groups = new List<UserWalletBrandHistoryGroupResponse>();

        if (response.IsCoinsEnabled)
        {
            groups.Add(BuildHistoryGroup(
                "Coin",
                "Монеты",
                response.Items.Where(item => item.SourceType == "Coin")));
        }

        if (response.IsMetricsEnabled)
        {
            groups.Add(BuildHistoryGroup(
                "Metric",
                "Метрики",
                response.Items.Where(item => item.SourceType == "Metric")));
        }

        return new UserWalletBrandHistorySectionResponse(
            "Последние операции",
            "Истории операций пока нет.",
            groups);
    }

    private static UserWalletBrandHistoryGroupResponse BuildHistoryGroup(
        string kind,
        string title,
        IEnumerable<UserBrandWalletHistoryItemResponse> items)
    {
        return new UserWalletBrandHistoryGroupResponse(
            kind,
            title,
            "Операций пока нет.",
            items
                .OrderByDescending(item => item.CreatedAt)
                .Select(item =>
                {
                    var isIssue = item.TransactionType == "Issue";
                    var comment = string.IsNullOrWhiteSpace(item.Comment) || IsAutoComment(item.Comment!)
                        ? null
                        : item.Comment;

                    return new UserWalletBrandHistoryItemDetailsResponse(
                        item.SourceType,
                        item.SourceName,
                        item.TransactionType,
                        item.Amount,
                        $"{(isIssue ? "+" : "-")}{item.Amount} {item.SourceName}",
                        comment,
                        comment is not null,
                        item.ActorUserId,
                        item.CreatedAt);
                })
                .ToArray());
    }

    private static bool IsAutoComment(string value)
    {
        return value is "Issue metric" or "Redeem metric" or "Issue coins" or "Redeem coins";
    }
}
