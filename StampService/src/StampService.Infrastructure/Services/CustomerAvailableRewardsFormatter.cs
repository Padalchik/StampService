using System.Net;
using Microsoft.EntityFrameworkCore;

namespace StampService.Infrastructure.Services;

public static class CustomerAvailableRewardsFormatter
{
    private const int MaxShownRewards = 5;

    public static async Task<string> BuildSectionAsync(
        AppDbContext dbContext,
        Guid userId,
        Guid brandId,
        CancellationToken cancellationToken)
    {
        var coinRewards = await dbContext.CoinWallets
            .AsNoTracking()
            .Where(wallet => wallet.UserId == userId
                && wallet.BrandId == brandId
                && wallet.Brand.IsCoinsEnabled
                && wallet.Brand.IsCoinProductRedemptionEnabled)
            .SelectMany(
                wallet => dbContext.CoinProducts
                    .Where(product => product.BrandId == wallet.BrandId
                        && product.IsActive
                        && product.Price <= wallet.Value),
                (wallet, product) => new AvailableReward(
                    product.Name,
                    product.Price,
                    false))
            .ToArrayAsync(cancellationToken);

        var metricRewards = await dbContext.MetricBalances
            .AsNoTracking()
            .Where(balance => balance.UserId == userId
                && balance.BrandId == brandId
                && balance.Brand.IsMetricsEnabled
                && balance.MetricDefinition.IsActive
                && balance.Value >= balance.MetricDefinition.RedemptionAmount)
            .Select(balance => new AvailableReward(
                balance.MetricDefinition.Name,
                balance.MetricDefinition.RedemptionAmount,
                true))
            .ToArrayAsync(cancellationToken);

        var rewards = coinRewards
            .Concat(metricRewards)
            .OrderBy(reward => reward.Price)
            .ThenBy(reward => reward.Name)
            .ToArray();

        if (rewards.Length == 0)
            return "\n\nСейчас доступных наград нет.";

        var shownRewards = rewards.Take(MaxShownRewards).ToArray();
        var section = "\n\n<b>Доступно сейчас</b>\n" + string.Join(
            "\n",
            shownRewards.Select(reward =>
                reward.IsMetric
                    ? $"- {Html(reward.Name)}: можно списать {reward.Price}"
                    : $"- {Html(reward.Name)} за {reward.Price} монеток"));

        var hiddenCount = rewards.Length - shownRewards.Length;
        if (hiddenCount > 0)
            section += $"\nИ ещё {hiddenCount} наград - откройте кошелёк, чтобы посмотреть все.";

        return section;
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    private sealed record AvailableReward(string Name, int Price, bool IsMetric);
}
