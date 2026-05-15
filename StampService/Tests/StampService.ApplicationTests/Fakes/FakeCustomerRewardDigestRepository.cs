using StampService.Application.CustomerNotifications;

namespace StampService.ApplicationTests.Fakes;

public class FakeCustomerRewardDigestRepository : ICustomerRewardDigestRepository
{
    private readonly Dictionary<Guid, CustomerRewardDigest> _digests = [];

    public void Set(CustomerRewardDigest digest)
    {
        _digests[digest.UserId] = digest;
    }

    public Task<CustomerRewardDigest> GetAvailableRewardsAsync(
        Guid userId,
        int maxBrands,
        int maxRewardsPerBrand,
        CancellationToken cancellationToken)
    {
        if (!_digests.TryGetValue(userId, out var digest))
            return Task.FromResult(new CustomerRewardDigest(userId, [], 0, 0));

        var brands = digest.Brands
            .Take(maxBrands)
            .Select(brand => brand with
            {
                Rewards = brand.Rewards.Take(maxRewardsPerBrand).ToArray()
            })
            .ToArray();

        return Task.FromResult(digest with
        {
            Brands = brands,
            ShownRewardCount = brands.Sum(brand => brand.Rewards.Count)
        });
    }
}
