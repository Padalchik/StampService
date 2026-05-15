using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Brands;
using StampService.Application.CoinProducts;
using StampService.Application.Coins;
using StampService.Application.Errors;
using StampService.Application.Metrics;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Wallet;

namespace StampService.Application.Wallet.Queries.GetUserWalletOverview;

public class GetUserWalletOverviewHandler
    : IQueryHandler<UserWalletOverviewResponse, GetUserWalletOverviewQuery>
{
    private readonly ICoinProductRepository _coinProductRepository;
    private readonly ICoinWalletRepository _coinWalletRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly IMetricBalanceRepository _metricBalanceRepository;
    private readonly IUserRepository _userRepository;

    public GetUserWalletOverviewHandler(
        ICoinProductRepository coinProductRepository,
        ICoinWalletRepository coinWalletRepository,
        IBrandRepository brandRepository,
        IMetricBalanceRepository metricBalanceRepository,
        IUserRepository userRepository)
    {
        _coinProductRepository = coinProductRepository;
        _coinWalletRepository = coinWalletRepository;
        _brandRepository = brandRepository;
        _metricBalanceRepository = metricBalanceRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<UserWalletOverviewResponse>> Handle(
        GetUserWalletOverviewQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        var userExists = await _userRepository.ExistsAsync(query.UserId, cancellationToken);
        if (!userExists)
            return Result.Fail(UserErrors.NotFound());

        var metricBalances = await _metricBalanceRepository.GetUserBalancesAsync(
            query.UserId,
            cancellationToken);
        var coinWallets = await _coinWalletRepository.GetUserWalletsAsync(
            query.UserId,
            cancellationToken);

        var brandIds = metricBalances
            .Select(balance => balance.BrandId)
            .Concat(coinWallets.Select(wallet => wallet.BrandId))
            .Distinct()
            .ToArray();

        var brands = new List<UserWalletBrandOverviewResponse>();
        foreach (var brandId in brandIds)
        {
            var brand = await _brandRepository.GetByIdAsync(brandId, cancellationToken);
            if (brand is null)
                continue;

            var brandMetricBalances = metricBalances
                .Where(balance => balance.BrandId == brandId)
                .ToArray();
            var coinWallet = coinWallets.FirstOrDefault(wallet => wallet.BrandId == brandId);
            var coinBalance = brand.IsCoinsEnabled ? coinWallet?.Value ?? 0 : 0;
            var brandName = brandMetricBalances.FirstOrDefault()?.BrandName
                ?? coinWallet?.BrandName
                ?? brand.Name;

            var activeProducts = brand.IsCoinsEnabled && brand.IsCoinProductRedemptionEnabled
                ? await _coinProductRepository.GetActiveByBrandAsync(
                    brandId,
                    cancellationToken)
                : [];

            brands.Add(new UserWalletBrandOverviewResponse(
                brandId,
                brandName,
                brand.IsMetricsEnabled,
                brand.IsCoinsEnabled,
                brand.IsCoinProductRedemptionEnabled,
                brand.IsManualCoinRedemptionEnabled,
                coinBalance,
                activeProducts
                    .Where(product => product.Price <= coinBalance)
                    .OrderBy(product => product.Price)
                    .ThenBy(product => product.Name)
                    .Select(product => new UserBrandCoinProductRewardResponse(
                        product.Id,
                        product.Name,
                        product.Price,
                        coinBalance,
                        MissingAmount: 0,
                        IsAvailable: true))
                    .ToArray(),
                (brand.IsMetricsEnabled ? brandMetricBalances : [])
                    .Where(balance => balance.Value >= balance.RedemptionAmount)
                    .OrderBy(balance => balance.MetricName)
                    .Select(balance => new UserBrandMetricRewardResponse(
                        balance.MetricDefinitionId,
                        balance.MetricName,
                        balance.Value,
                        balance.RedemptionAmount,
                        MissingAmount: 0,
                        IsAvailable: true))
                    .ToArray()));
        }

        return Result.Ok(new UserWalletOverviewResponse(
            query.UserId,
            brands
                .OrderBy(brand => brand.BrandName)
                .ToArray()));
    }
}
