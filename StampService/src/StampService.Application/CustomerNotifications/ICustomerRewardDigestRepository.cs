namespace StampService.Application.CustomerNotifications;

public interface ICustomerRewardDigestRepository
{
    Task<CustomerRewardDigest> GetAvailableRewardsAsync(
        Guid userId,
        int maxBrands,
        int maxRewardsPerBrand,
        CancellationToken cancellationToken);
}
