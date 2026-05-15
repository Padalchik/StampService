using StampService.Application.CustomerNotifications;
using StampService.Application.CustomerNotifications.Queries.GetCustomerRewardDigest;
using StampService.ApplicationTests.Fakes;

namespace StampService.ApplicationTests.CustomerNotifications;

public class GetCustomerRewardDigestHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnGroupedRewardsWithLimits()
    {
        var userId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var repository = new FakeCustomerRewardDigestRepository();
        repository.Set(new CustomerRewardDigest(
            userId,
            [
                new CustomerRewardDigestBrand(
                    brandId,
                    "Coffee",
                    [
                        new CustomerRewardDigestReward("Latte", 5, "монеток"),
                        new CustomerRewardDigestReward("Raf", 7, "монеток")
                    ])
            ],
            TotalRewardCount: 2,
            ShownRewardCount: 2));
        var handler = new GetCustomerRewardDigestHandler(repository);

        var result = await handler.Handle(
            new GetCustomerRewardDigestQuery(userId, MaxBrands: 5, MaxRewardsPerBrand: 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var brand = Assert.Single(result.Value.Brands);
        Assert.Equal("Coffee", brand.BrandName);
        var reward = Assert.Single(brand.Rewards);
        Assert.Equal("Latte", reward.RewardName);
        Assert.Equal(2, result.Value.TotalRewardCount);
        Assert.Equal(1, result.Value.ShownRewardCount);
        Assert.Equal(1, result.Value.HiddenRewardCount);
    }
}
