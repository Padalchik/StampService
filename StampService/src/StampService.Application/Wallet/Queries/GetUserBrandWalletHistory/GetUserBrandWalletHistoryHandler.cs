using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Brands;
using StampService.Application.Coins;
using StampService.Application.Errors;
using StampService.Application.Metrics;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Wallet;

namespace StampService.Application.Wallet.Queries.GetUserBrandWalletHistory;

public class GetUserBrandWalletHistoryHandler
    : IQueryHandler<UserBrandWalletHistoryResponse, GetUserBrandWalletHistoryQuery>
{
    private const int MaxTake = 100;

    private readonly ICoinTransactionRepository _coinTransactionRepository;
    private readonly ICoinWalletRepository _coinWalletRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly IMetricBalanceRepository _metricBalanceRepository;
    private readonly IStampTransactionRepository _stampTransactionRepository;
    private readonly IUserRepository _userRepository;

    public GetUserBrandWalletHistoryHandler(
        ICoinTransactionRepository coinTransactionRepository,
        ICoinWalletRepository coinWalletRepository,
        IBrandRepository brandRepository,
        IMetricBalanceRepository metricBalanceRepository,
        IStampTransactionRepository stampTransactionRepository,
        IUserRepository userRepository)
    {
        _coinTransactionRepository = coinTransactionRepository;
        _coinWalletRepository = coinWalletRepository;
        _brandRepository = brandRepository;
        _metricBalanceRepository = metricBalanceRepository;
        _stampTransactionRepository = stampTransactionRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<UserBrandWalletHistoryResponse>> Handle(
        GetUserBrandWalletHistoryQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Skip < 0)
            return Result.Fail(PagingErrors.SkipCannotBeNegative());

        if (query.Take <= 0 || query.Take > MaxTake)
            return Result.Fail(PagingErrors.TakeOutOfRange(MaxTake));

        var userExists = await _userRepository.ExistsAsync(query.UserId, cancellationToken);
        if (!userExists)
            return Result.Fail(UserErrors.NotFound());

        var brand = await _brandRepository.GetByIdAsync(query.BrandId, cancellationToken);
        if (brand is null)
            return Result.Fail(BrandErrors.NotFound());

        var sourceTake = query.Skip + query.Take;
        var balances = brand.IsMetricsEnabled
            ? (await _metricBalanceRepository.GetUserBalancesAsync(query.UserId, cancellationToken))
                .Where(balance => balance.BrandId == query.BrandId)
                .ToArray()
            : [];

        var items = new List<UserBrandWalletHistoryItemResponse>();
        foreach (var balance in balances)
        {
            var transactions = await _stampTransactionRepository.GetHistoryByMetricBalanceAsync(
                balance.BalanceId,
                skip: 0,
                take: sourceTake,
                cancellationToken);

            items.AddRange(transactions.Select(transaction => new UserBrandWalletHistoryItemResponse(
                SourceType: "Metric",
                SourceName: balance.MetricName,
                TransactionType: transaction.Type.ToString(),
                Amount: transaction.Amount,
                Comment: transaction.Comment,
                ActorUserId: transaction.ActorUserId,
                CreatedAt: transaction.CreatedAt)));
        }

        var wallet = brand.IsCoinsEnabled
            ? await _coinWalletRepository.GetByUserAndBrandAsync(
                query.UserId,
                query.BrandId,
                cancellationToken)
            : null;

        if (wallet is not null)
        {
            var transactions = await _coinTransactionRepository.GetHistoryByWalletAsync(
                wallet.Id,
                skip: 0,
                take: sourceTake,
                cancellationToken);

            items.AddRange(transactions.Select(transaction => new UserBrandWalletHistoryItemResponse(
                SourceType: "Coin",
                SourceName: "монетки",
                TransactionType: transaction.Type.ToString(),
                Amount: transaction.Amount,
                Comment: transaction.Comment,
                ActorUserId: transaction.ActorUserId,
                CreatedAt: transaction.CreatedAt)));
        }

        var brandName = balances.FirstOrDefault()?.BrandName
            ?? (brand.IsCoinsEnabled
                ? (await _coinWalletRepository.GetUserWalletsAsync(query.UserId, cancellationToken))
                    .FirstOrDefault(wallet => wallet.BrandId == query.BrandId)
                    ?.BrandName
                : null)
            ?? brand.Name;

        var pageItems = items
            .OrderByDescending(item => item.CreatedAt)
            .Skip(query.Skip)
            .Take(query.Take)
            .ToArray();

        return Result.Ok(new UserBrandWalletHistoryResponse(
            query.UserId,
            query.BrandId,
            brandName,
            brand.IsMetricsEnabled,
            brand.IsCoinsEnabled,
            query.Skip,
            query.Take,
            pageItems));
    }
}
