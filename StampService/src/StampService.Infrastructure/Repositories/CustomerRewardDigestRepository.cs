using Microsoft.EntityFrameworkCore;
using StampService.Application.CustomerNotifications;

namespace StampService.Infrastructure.Repositories;

public class CustomerRewardDigestRepository : ICustomerRewardDigestRepository
{
    private const string CoinUnitName = "монеток";
    private const string MetricUnitName = "метрик";

    private readonly AppDbContext _dbContext;

    public CustomerRewardDigestRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CustomerRewardDigest> GetAvailableRewardsAsync(
        Guid userId,
        int maxBrands,
        int maxRewardsPerBrand,
        CancellationToken cancellationToken)
    {
        maxBrands = Math.Max(1, maxBrands);
        maxRewardsPerBrand = Math.Max(1, maxRewardsPerBrand);

        var coinRewards = await _dbContext.CoinWallets
            .AsNoTracking()
            .Where(wallet => wallet.UserId == userId
                && wallet.Brand.IsCoinsEnabled
                && wallet.Brand.IsCoinProductRedemptionEnabled)
            .SelectMany(
                wallet => _dbContext.CoinProducts
                    .Where(product => product.BrandId == wallet.BrandId
                        && product.IsActive
                        && product.Price <= wallet.Value),
                (wallet, product) => new RewardCandidate(
                    wallet.BrandId,
                    wallet.Brand.Name,
                    product.Name,
                    product.Price,
                    CoinUnitName))
            .ToArrayAsync(cancellationToken);

        var metricRewards = await _dbContext.MetricBalances
            .AsNoTracking()
            .Where(balance => balance.UserId == userId
                && balance.Brand.IsMetricsEnabled
                && balance.MetricDefinition.IsActive
                && balance.Value >= balance.MetricDefinition.RedemptionAmount)
            .Select(balance => new RewardCandidate(
                balance.BrandId,
                balance.Brand.Name,
                balance.MetricDefinition.Name,
                balance.MetricDefinition.RedemptionAmount,
                MetricUnitName))
            .ToArrayAsync(cancellationToken);

        var allRewards = coinRewards
            .Concat(metricRewards)
            .OrderBy(reward => reward.BrandName)
            .ThenBy(reward => reward.Price)
            .ThenBy(reward => reward.RewardName)
            .ToArray();

        var shownBrands = allRewards
            .GroupBy(reward => new { reward.BrandId, reward.BrandName })
            .OrderBy(group => group.Key.BrandName)
            .Take(maxBrands)
            .Select(group => new CustomerRewardDigestBrand(
                group.Key.BrandId,
                group.Key.BrandName,
                group
                    .Take(maxRewardsPerBrand)
                    .Select(reward => new CustomerRewardDigestReward(
                        reward.RewardName,
                        reward.Price,
                        reward.UnitName))
                    .ToArray()))
            .ToArray();

        var shownRewardCount = shownBrands.Sum(brand => brand.Rewards.Count);

        return new CustomerRewardDigest(
            userId,
            shownBrands,
            allRewards.Length,
            shownRewardCount);
    }

    private sealed record RewardCandidate(
        Guid BrandId,
        string BrandName,
        string RewardName,
        int Price,
        string UnitName);
}
