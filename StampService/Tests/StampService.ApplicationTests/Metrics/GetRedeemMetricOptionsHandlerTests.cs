using StampService.Application.Access;
using StampService.Application.Metrics.Queries.GetRedeemMetricOptions;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Access;
using StampService.Domain.Loyalty;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Metrics;

public class GetRedeemMetricOptionsHandlerTests
{
    [Fact]
    public async Task Handle_WhenCodeIsActive_ShouldReturnMetricsWithRedeemAvailability()
    {
        var now = new DateTimeOffset(2026, 5, 8, 12, 0, 0, TimeSpan.Zero);
        var brandId = Guid.NewGuid();
        var redeemer = User.Create("Staff", "1001").Value;
        var customer = User.Create("Customer", "2002").Value;
        var availableMetric = LoyaltyMetricDefinition.Create(brandId, "coffee", "Coffee", 2).Value;
        var unavailableMetric = LoyaltyMetricDefinition.Create(brandId, "cake", "Cake", 5).Value;
        var userRepository = new FakeUserRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        var metricRepository = new FakeLoyaltyMetricRepository();
        var balanceRepository = new FakeMetricBalanceRepository();
        var codeRepository = new FakeRedemptionCodeRepository();

        userRepository.Add(redeemer);
        userRepository.Add(customer);
        membershipRepository.SetRole(redeemer.Id, brandId, SystemRoles.Staff);
        metricRepository.AddExisting(availableMetric);
        metricRepository.AddExisting(unavailableMetric);
        codeRepository.Add(RedemptionCode.Create(
            customer.Id,
            "1234",
            now.UtcDateTime.AddMinutes(5),
            now.UtcDateTime).Value);

        var balance = MetricBalance.Create(customer.Id, brandId, availableMetric.Id).Value;
        balance.SetMaterializedValue(3);
        balanceRepository.Add(balance);

        var handler = new GetRedeemMetricOptionsHandler(
            new BrandAccessService(membershipRepository),
            metricRepository,
            balanceRepository,
            codeRepository,
            userRepository,
            new FixedTimeProvider(now));

        var result = await handler.Handle(
            new GetRedeemMetricOptionsQuery(redeemer.Id, brandId, "1234"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(customer.Id, result.Value.CustomerUserId);
        Assert.Equal("1234", result.Value.RedemptionCode);
        Assert.Equal(2, result.Value.Metrics.Count);

        var available = result.Value.Metrics.Single(metric => metric.MetricDefinitionId == availableMetric.Id);
        Assert.True(available.CanRedeem);
        Assert.Equal(3, available.CurrentBalance);
        Assert.Equal(2, available.RequiredAmount);

        var unavailable = result.Value.Metrics.Single(metric => metric.MetricDefinitionId == unavailableMetric.Id);
        Assert.False(unavailable.CanRedeem);
        Assert.Equal(0, unavailable.CurrentBalance);
        Assert.Equal(5, unavailable.RequiredAmount);
    }

    [Fact]
    public async Task Handle_WhenRedeemerCannotRedeem_ShouldFail()
    {
        var now = new DateTimeOffset(2026, 5, 8, 12, 0, 0, TimeSpan.Zero);
        var handler = new GetRedeemMetricOptionsHandler(
            new BrandAccessService(new FakeBrandMembershipRepository()),
            new FakeLoyaltyMetricRepository(),
            new FakeMetricBalanceRepository(),
            new FakeRedemptionCodeRepository(),
            new FakeUserRepository(),
            new FixedTimeProvider(now));

        var result = await handler.Handle(
            new GetRedeemMetricOptionsQuery(Guid.NewGuid(), Guid.NewGuid(), "1234"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }
}
