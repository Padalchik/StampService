using StampService.Application.Access;
using StampService.Application.Metrics.Queries.GetBrandManageMetrics;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Access;
using StampService.Domain.Loyalty;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Metrics;

public class GetBrandManageMetricsHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserCanManageMetrics_ShouldReturnActiveAndInactiveMetrics()
    {
        var user = User.Create("Owner", "1234").Value;
        var brandId = Guid.NewGuid();
        var activeMetric = LoyaltyMetricDefinition.Create(brandId, "Active", 1).Value;
        var inactiveMetric = LoyaltyMetricDefinition.Create(brandId, "Inactive", 2).Value;
        inactiveMetric.Deactivate();

        var membershipRepository = new FakeBrandMembershipRepository();
        membershipRepository.SetRole(user.Id, brandId, SystemRoles.Owner);
        var metricRepository = new FakeLoyaltyMetricRepository();
        metricRepository.AddExisting(activeMetric);
        metricRepository.AddExisting(inactiveMetric);
        var userRepository = new FakeUserRepository();
        userRepository.Add(user);

        var handler = new GetBrandManageMetricsHandler(
            new BrandAccessService(membershipRepository),
            metricRepository,
            userRepository);

        var result = await handler.Handle(
            new GetBrandManageMetricsQuery(user.Id, brandId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(result.Value, metric => metric.Id == inactiveMetric.Id && !metric.IsActive);
    }

    [Fact]
    public async Task Handle_WhenUserCannotManageMetrics_ShouldFail()
    {
        var user = User.Create("Staff", "1234").Value;
        var brandId = Guid.NewGuid();
        var membershipRepository = new FakeBrandMembershipRepository();
        membershipRepository.SetRole(user.Id, brandId, SystemRoles.Staff);
        var userRepository = new FakeUserRepository();
        userRepository.Add(user);

        var handler = new GetBrandManageMetricsHandler(
            new BrandAccessService(membershipRepository),
            new FakeLoyaltyMetricRepository(),
            userRepository);

        var result = await handler.Handle(
            new GetBrandManageMetricsQuery(user.Id, brandId),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }
}
