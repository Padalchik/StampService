using StampService.Application.Access;
using StampService.Application.Metrics.Queries.GetBrandIssueMetrics;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Access;
using StampService.Domain.Loyalty;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Metrics;

public class GetBrandIssueMetricsHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserCanIssue_ShouldReturnActiveBrandMetrics()
    {
        var user = User.Create("issuer").Value;
        var brandId = Guid.NewGuid();
        var membershipRepository = new FakeBrandMembershipRepository();
        var metricRepository = new FakeLoyaltyMetricRepository();
        var userRepository = new FakeUserRepository();
        userRepository.Add(user);
        membershipRepository.SetRole(user.Id, brandId, SystemRoles.Staff, "Coffee");
        metricRepository.AddExisting(LoyaltyMetricDefinition.Create(brandId, "stamp", "Stamps").Value);
        var inactiveMetric = LoyaltyMetricDefinition.Create(brandId, "old", "Old").Value;
        inactiveMetric.Deactivate();
        metricRepository.AddExisting(inactiveMetric);
        metricRepository.AddExisting(LoyaltyMetricDefinition.Create(Guid.NewGuid(), "other", "Other").Value);
        var handler = new GetBrandIssueMetricsHandler(
            new BrandAccessService(membershipRepository),
            metricRepository,
            userRepository);

        var result = await handler.Handle(
            new GetBrandIssueMetricsQuery(user.Id, brandId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("stamp", result.Value.Single().Code);
    }

    [Fact]
    public async Task Handle_WhenUserCannotIssue_ShouldFail()
    {
        var user = User.Create("issuer").Value;
        var userRepository = new FakeUserRepository();
        userRepository.Add(user);
        var handler = new GetBrandIssueMetricsHandler(
            new BrandAccessService(new FakeBrandMembershipRepository()),
            new FakeLoyaltyMetricRepository(),
            userRepository);

        var result = await handler.Handle(
            new GetBrandIssueMetricsQuery(user.Id, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldFail()
    {
        var handler = new GetBrandIssueMetricsHandler(
            new BrandAccessService(new FakeBrandMembershipRepository()),
            new FakeLoyaltyMetricRepository(),
            new FakeUserRepository());

        var result = await handler.Handle(
            new GetBrandIssueMetricsQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task Handle_WhenUserIdIsEmpty_ShouldFail()
    {
        var handler = new GetBrandIssueMetricsHandler(
            new BrandAccessService(new FakeBrandMembershipRepository()),
            new FakeLoyaltyMetricRepository(),
            new FakeUserRepository());

        var result = await handler.Handle(
            new GetBrandIssueMetricsQuery(Guid.Empty, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task Handle_WhenBrandIdIsEmpty_ShouldFail()
    {
        var user = User.Create("issuer").Value;
        var userRepository = new FakeUserRepository();
        userRepository.Add(user);
        var handler = new GetBrandIssueMetricsHandler(
            new BrandAccessService(new FakeBrandMembershipRepository()),
            new FakeLoyaltyMetricRepository(),
            userRepository);

        var result = await handler.Handle(
            new GetBrandIssueMetricsQuery(user.Id, Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }
}
