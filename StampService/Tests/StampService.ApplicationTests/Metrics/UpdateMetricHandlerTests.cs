using StampService.Application.Access;
using StampService.Application.Metrics.Commands.UpdateMetric;
using StampService.ApplicationTests.Fakes;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Access;
using StampService.Domain.Loyalty;

namespace StampService.ApplicationTests.Metrics;

public class UpdateMetricHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserCanManageMetrics_ShouldUpdateMetric()
    {
        var userId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var metric = LoyaltyMetricDefinition.Create(brandId, "Old", 1).Value;
        var membershipRepository = new FakeBrandMembershipRepository();
        membershipRepository.SetRole(userId, brandId, SystemRoles.Owner);
        var metricRepository = new FakeLoyaltyMetricRepository();
        metricRepository.AddExisting(metric);

        var handler = new UpdateMetricHandler(
            new BrandAccessService(membershipRepository),
            metricRepository);

        var result = await handler.Handle(
            new UpdateMetricCommand(
                metric.Id,
                userId,
                new UpdateMetricRequest("New", 3)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New", result.Value.Name);
        Assert.Equal(3, result.Value.RedemptionAmount);
        Assert.True(result.Value.IsActive);
    }
}
