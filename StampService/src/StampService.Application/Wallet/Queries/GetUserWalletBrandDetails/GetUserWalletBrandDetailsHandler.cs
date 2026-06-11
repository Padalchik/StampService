using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Brands;
using StampService.Application.CoinProducts;
using StampService.Application.Coins;
using StampService.Application.Errors;
using StampService.Application.Metrics;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Wallet;
using StampService.Domain.Coins;
using StampService.Domain.Loyalty;

namespace StampService.Application.Wallet.Queries.GetUserWalletBrandDetails;

public class GetUserWalletBrandDetailsHandler
    : IQueryHandler<UserWalletBrandDetailsResponse, GetUserWalletBrandDetailsQuery>
{
    private const int HistoryTake = 10;

    private readonly ICoinProductRepository _coinProductRepository;
    private readonly ICoinTransactionRepository _coinTransactionRepository;
    private readonly ICoinWalletRepository _coinWalletRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly ILoyaltyMetricRepository _metricRepository;
    private readonly IMetricBalanceRepository _metricBalanceRepository;
    private readonly IStampTransactionRepository _stampTransactionRepository;
    private readonly IUserRepository _userRepository;

    public GetUserWalletBrandDetailsHandler(
        ICoinProductRepository coinProductRepository,
        ICoinTransactionRepository coinTransactionRepository,
        ICoinWalletRepository coinWalletRepository,
        IBrandRepository brandRepository,
        ILoyaltyMetricRepository metricRepository,
        IMetricBalanceRepository metricBalanceRepository,
        IStampTransactionRepository stampTransactionRepository,
        IUserRepository userRepository)
    {
        _coinProductRepository = coinProductRepository;
        _coinTransactionRepository = coinTransactionRepository;
        _coinWalletRepository = coinWalletRepository;
        _brandRepository = brandRepository;
        _metricRepository = metricRepository;
        _metricBalanceRepository = metricBalanceRepository;
        _stampTransactionRepository = stampTransactionRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<UserWalletBrandDetailsResponse>> Handle(
        GetUserWalletBrandDetailsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        var userExists = await _userRepository.ExistsAsync(query.UserId, cancellationToken);
        if (!userExists)
            return Result.Fail(UserErrors.NotFound());

        var brand = await _brandRepository.GetByIdAsync(query.BrandId, cancellationToken);
        if (brand is null)
            return Result.Fail(BrandErrors.NotFound());

        var balances = brand.IsMetricsEnabled
            ? (await _metricBalanceRepository.GetUserBalancesAsync(query.UserId, cancellationToken))
                .Where(balance => balance.BrandId == query.BrandId)
                .ToArray()
            : Array.Empty<UserMetricBalanceReadModel>();

        var metrics = brand.IsMetricsEnabled
            ? (await _metricRepository.GetByBrandAsync(query.BrandId, cancellationToken))
                .Where(metric => metric.IsActive)
                .ToArray()
            : Array.Empty<LoyaltyMetricDefinition>();

        var wallet = brand.IsCoinsEnabled
            ? await _coinWalletRepository.GetByUserAndBrandAsync(
                query.UserId,
                query.BrandId,
                cancellationToken)
            : null;

        var coinWallets = brand.IsCoinsEnabled
            ? await _coinWalletRepository.GetUserWalletsAsync(query.UserId, cancellationToken)
            : Array.Empty<UserCoinWalletReadModel>();
        var walletReadModel = coinWallets.FirstOrDefault(coinWallet => coinWallet.BrandId == query.BrandId);
        var brandName = balances.FirstOrDefault()?.BrandName
            ?? walletReadModel?.BrandName
            ?? brand.Name;
        var coinBalance = wallet?.Value ?? walletReadModel?.Value ?? 0;

        var products = brand.IsCoinsEnabled && brand.IsCoinProductRedemptionEnabled
            ? await _coinProductRepository.GetActiveByBrandAsync(query.BrandId, cancellationToken)
            : Array.Empty<CoinProduct>();

        var historyItems = await LoadHistoryItemsAsync(
            brand.IsCoinsEnabled,
            balances,
            wallet,
            cancellationToken);

        return Result.Ok(new UserWalletBrandDetailsResponse(
            query.UserId,
            query.BrandId,
            brandName,
            brand.IsMetricsEnabled,
            brand.IsCoinsEnabled,
            brand.IsCoinProductRedemptionEnabled,
            coinBalance,
            BuildRewardSections(
                brand.IsCoinsEnabled,
                brand.IsMetricsEnabled,
                brand.IsCoinProductRedemptionEnabled,
                coinBalance,
                products,
                metrics,
                balances),
            BuildHistorySection(brand.IsCoinsEnabled, brand.IsMetricsEnabled, historyItems),
            "Чтобы получить награду, покажите код для списания сотруднику."));
    }

    private async Task<IReadOnlyCollection<WalletBrandHistoryItem>> LoadHistoryItemsAsync(
        bool isCoinsEnabled,
        IReadOnlyCollection<UserMetricBalanceReadModel> balances,
        CoinWallet? wallet,
        CancellationToken cancellationToken)
    {
        var items = new List<WalletBrandHistoryItem>();
        foreach (var balance in balances)
        {
            var transactions = await _stampTransactionRepository.GetHistoryByMetricBalanceAsync(
                balance.BalanceId,
                skip: 0,
                take: HistoryTake,
                cancellationToken);

            items.AddRange(transactions.Select(transaction => new WalletBrandHistoryItem(
                SourceType: "Metric",
                SourceName: balance.MetricName,
                TransactionType: transaction.Type.ToString(),
                Amount: transaction.Amount,
                Comment: transaction.Comment,
                ActorUserId: transaction.ActorUserId,
                CreatedAt: transaction.CreatedAt)));
        }

        if (isCoinsEnabled && wallet is not null)
        {
            var transactions = await _coinTransactionRepository.GetHistoryByWalletAsync(
                wallet.Id,
                skip: 0,
                take: HistoryTake,
                cancellationToken);

            items.AddRange(transactions.Select(transaction => new WalletBrandHistoryItem(
                SourceType: "Coin",
                SourceName: "монетки",
                TransactionType: transaction.Type.ToString(),
                Amount: transaction.Amount,
                Comment: transaction.Comment,
                ActorUserId: transaction.ActorUserId,
                CreatedAt: transaction.CreatedAt)));
        }

        return items
            .OrderByDescending(item => item.CreatedAt)
            .Take(HistoryTake)
            .ToArray();
    }

    private static IReadOnlyCollection<UserWalletBrandRewardSectionResponse> BuildRewardSections(
        bool isCoinsEnabled,
        bool isMetricsEnabled,
        bool isCoinProductRedemptionEnabled,
        int coinBalance,
        IEnumerable<CoinProduct> products,
        IEnumerable<LoyaltyMetricDefinition> metrics,
        IEnumerable<UserMetricBalanceReadModel> balances)
    {
        var sections = new List<UserWalletBrandRewardSectionResponse>();

        if (isCoinsEnabled && isCoinProductRedemptionEnabled)
        {
            sections.Add(new UserWalletBrandRewardSectionResponse(
                "CoinProducts",
                "Товары за монетки",
                $"Монетки: {coinBalance}",
                "Пока нет активных товаров.",
                products
                    .OrderBy(product => product.Price)
                    .ThenBy(product => product.Name)
                    .Select(product =>
                    {
                        var missingAmount = Math.Max(0, product.Price - coinBalance);
                        return new UserWalletBrandRewardItemResponse(
                            product.Id,
                            product.Name,
                            $"{coinBalance}/{product.Price}",
                            missingAmount == 0 ? "доступно" : $"не хватает {missingAmount}",
                            missingAmount == 0);
                    })
                    .ToArray()));
        }

        if (isMetricsEnabled)
        {
            var balancesByMetric = balances.ToDictionary(balance => balance.MetricDefinitionId);

            sections.Add(new UserWalletBrandRewardSectionResponse(
                "Metrics",
                "Метрики",
                null,
                "Пока нет метрик.",
                metrics
                    .OrderBy(metric => metric.Name)
                    .Select(metric =>
                    {
                        balancesByMetric.TryGetValue(metric.Id, out var balance);
                        var currentValue = balance?.Value ?? 0;
                        var missingAmount = Math.Max(0, metric.RedemptionAmount - currentValue);

                        return new UserWalletBrandRewardItemResponse(
                            metric.Id,
                            metric.Name,
                            $"{currentValue}/{metric.RedemptionAmount}",
                            missingAmount == 0 ? "доступно" : $"не хватает {missingAmount}",
                            missingAmount == 0);
                    })
                    .ToArray()));
        }

        return sections;
    }

    private static UserWalletBrandHistorySectionResponse BuildHistorySection(
        bool isCoinsEnabled,
        bool isMetricsEnabled,
        IReadOnlyCollection<WalletBrandHistoryItem> items)
    {
        var groups = new List<UserWalletBrandHistoryGroupResponse>();

        if (isCoinsEnabled)
        {
            groups.Add(BuildHistoryGroup(
                "Coin",
                "Монеты",
                items.Where(item => item.SourceType == "Coin")));
        }

        if (isMetricsEnabled)
        {
            groups.Add(BuildHistoryGroup(
                "Metric",
                "Метрики",
                items.Where(item => item.SourceType == "Metric")));
        }

        return new UserWalletBrandHistorySectionResponse(
            "Последние операции",
            "Истории операций пока нет.",
            groups);
    }

    private static UserWalletBrandHistoryGroupResponse BuildHistoryGroup(
        string kind,
        string title,
        IEnumerable<WalletBrandHistoryItem> items)
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

    private sealed record WalletBrandHistoryItem(
        string SourceType,
        string SourceName,
        string TransactionType,
        int Amount,
        string? Comment,
        Guid ActorUserId,
        DateTime CreatedAt);
}
