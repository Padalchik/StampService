using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.CoinProducts;
using StampService.Application.Coins;
using StampService.Application.Errors;
using StampService.Application.Metrics;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Wallet;

namespace StampService.Application.Wallet.Queries.GetUserBrandRewards;

public class GetUserBrandRewardsHandler
    : IQueryHandler<UserBrandRewardsResponse, GetUserBrandRewardsQuery>
{
    private readonly ICoinProductRepository _coinProductRepository;
    private readonly ICoinWalletRepository _coinWalletRepository;
    private readonly IMetricBalanceRepository _metricBalanceRepository;
    private readonly IUserRepository _userRepository;

    public GetUserBrandRewardsHandler(
        ICoinProductRepository coinProductRepository,
        ICoinWalletRepository coinWalletRepository,
        IMetricBalanceRepository metricBalanceRepository,
        IUserRepository userRepository)
    {
        _coinProductRepository = coinProductRepository;
        _coinWalletRepository = coinWalletRepository;
        _metricBalanceRepository = metricBalanceRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<UserBrandRewardsResponse>> Handle(
        GetUserBrandRewardsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        var userExists = await _userRepository.ExistsAsync(query.UserId, cancellationToken);
        if (!userExists)
            return Result.Fail(UserErrors.NotFound());

        var balances = (await _metricBalanceRepository.GetUserBalancesAsync(query.UserId, cancellationToken))
            .Where(balance => balance.BrandId == query.BrandId)
            .ToArray();

        var wallet = await _coinWalletRepository.GetByUserAndBrandAsync(
            query.UserId,
            query.BrandId,
            cancellationToken);

        var coinWallets = await _coinWalletRepository.GetUserWalletsAsync(query.UserId, cancellationToken);
        var walletReadModel = coinWallets.FirstOrDefault(coinWallet => coinWallet.BrandId == query.BrandId);
        var brandName = balances.FirstOrDefault()?.BrandName
            ?? walletReadModel?.BrandName
            ?? "бренд";
        var coinBalance = wallet?.Value ?? walletReadModel?.Value ?? 0;

        var products = await _coinProductRepository.GetActiveByBrandAsync(query.BrandId, cancellationToken);

        return Result.Ok(new UserBrandRewardsResponse(
            query.UserId,
            query.BrandId,
            brandName,
            coinBalance,
            products
                .OrderBy(product => product.Price)
                .ThenBy(product => product.Name)
                .Select(product =>
                {
                    var missingAmount = Math.Max(0, product.Price - coinBalance);
                    return new UserBrandCoinProductRewardResponse(
                        product.Id,
                        product.Name,
                        product.Price,
                        coinBalance,
                        missingAmount,
                        missingAmount == 0);
                })
                .ToArray(),
            balances
                .OrderBy(balance => balance.MetricName)
                .Select(balance =>
                {
                    var missingAmount = Math.Max(0, balance.RedemptionAmount - balance.Value);
                    return new UserBrandMetricRewardResponse(
                        balance.MetricDefinitionId,
                        balance.MetricName,
                        balance.Value,
                        balance.RedemptionAmount,
                        missingAmount,
                        missingAmount == 0);
                })
                .ToArray()));
    }
}
