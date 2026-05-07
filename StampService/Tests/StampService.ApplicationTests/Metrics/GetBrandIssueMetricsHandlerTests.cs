using StampService.Application.Access;
using StampService.Application.Metrics.Queries.GetBrandIssueMetrics;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Access;
using StampService.Domain.Loyalty;

namespace StampService.ApplicationTests.Metrics;

public class GetBrandIssueMetricsHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserCanIssue_ShouldReturnActiveBrandMetrics()
    {
        var userId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var membershipRepository = new FakeBrandMembershipRepository();
        var metricRepository = new FakeLoyaltyMetricRepository();
        membershipRepository.SetRole(userId, brandId, SystemRoles.Staff, "Coffee");
        metricRepository.AddExisting(LoyaltyMetricDefinition.Create(brandId, "stamp", "Stamps").Value);
        var inactiveMetric = LoyaltyMetricDefinition.Create(brandId, "old", "Old").Value;
        inactiveMetric.Deactivate();
        metricRepository.AddExisting(inactiveMetric);
        var handler = new GetBrandIssueMetricsHandler(
            new BrandAccessService(membershipRepository),
            metricRepository);

        var result = await handler.Handle(
            new GetBrandIssueMetricsQuery(userId, brandId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("stamp", result.Value.Single().Code);
    }

    [Fact]
    public async Task Handle_WhenUserCannotIssue_ShouldFail()
    {
        var handler = new GetBrandIssueMetricsHandler(
            new BrandAccessService(new FakeBrandMembershipRepository()),
            new FakeLoyaltyMetricRepository());

        var result = await handler.Handle(
            new GetBrandIssueMetricsQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }
}
