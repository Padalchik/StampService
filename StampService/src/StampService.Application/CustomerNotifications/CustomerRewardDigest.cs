namespace StampService.Application.CustomerNotifications;

public sealed record CustomerRewardDigest(
    Guid UserId,
    IReadOnlyCollection<CustomerRewardDigestBrand> Brands,
    int TotalRewardCount,
    int ShownRewardCount)
{
    public int HiddenRewardCount => Math.Max(0, TotalRewardCount - ShownRewardCount);
}

public sealed record CustomerRewardDigestBrand(
    Guid BrandId,
    string BrandName,
    IReadOnlyCollection<CustomerRewardDigestReward> Rewards);

public sealed record CustomerRewardDigestReward(
    string RewardName,
    int Price,
    string UnitName);
