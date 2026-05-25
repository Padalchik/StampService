using StampService.Application.Access;
using StampService.Application.Metrics.Queries.GetBrandRedeemMetrics;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Access;
using StampService.Domain.Loyalty;
using DomainUser = StampService.Domain.User.User;

namespace StampService.ApplicationTests.Metrics;

public class GetBrandRedeemMetricsHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserCanRedeem_ShouldReturnActiveBrandMetrics()
    {
        var brandId = Guid.NewGuid();
        var user = DomainUser.Create("Ivan").Value;
        var userRepository = new FakeUserRepository();
        userRepository.Add(user);
        var membershipRepository = new FakeBrandMembershipRepository();
        membershipRepository.SetRole(user.Id, brandId, SystemRoles.Staff);
        var metricRepository = new FakeLoyaltyMetricRepository();
        metricRepository.AddExisting(LoyaltyMetricDefinition.Create(brandId, "Coffee", 2).Value);

        var handler = new GetBrandRedeemMetricsHandler(
            new BrandAccessService(membershipRepository),
            metricRepository,
            userRepository);

        var result = await handler.Handle(
            new GetBrandRedeemMetricsQuery(user.Id, brandId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("Coffee", result.Value.Single().Name);
        Assert.Equal(2, result.Value.Single().RedemptionAmount);
    }

    [Fact]
    public async Task Handle_WhenUserCannotRedeem_ShouldFail()
    {
        var brandId = Guid.NewGuid();
        var user = DomainUser.Create("Ivan").Value;
        var userRepository = new FakeUserRepository();
        userRepository.Add(user);

        var handler = new GetBrandRedeemMetricsHandler(
            new BrandAccessService(new FakeBrandMembershipRepository()),
            new FakeLoyaltyMetricRepository(),
            userRepository);

        var result = await handler.Handle(
            new GetBrandRedeemMetricsQuery(user.Id, brandId),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }
}
