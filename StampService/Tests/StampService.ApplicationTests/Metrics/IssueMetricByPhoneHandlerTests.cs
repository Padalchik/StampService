using StampService.Application.Access;
using StampService.Application.Metrics;
using StampService.Application.Metrics.Commands.IssueMetric;
using StampService.Application.Users;
using StampService.ApplicationTests.Fakes;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Access;
using StampService.Domain.Brand;
using StampService.Domain.Loyalty;

namespace StampService.ApplicationTests.Metrics;

public class IssueMetricByPhoneHandlerTests
{
    [Fact]
    public async Task Handle_WhenPhoneUserDoesNotExist_ShouldCreateUserAndIssueMetric()
    {
        var brand = Brand.Create("Coffee").Value;
        var metric = LoyaltyMetricDefinition.Create(brand.Id, "Coffee stamp", redemptionAmount: 6).Value;
        var actorUserId = Guid.NewGuid();
        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        var metricRepository = new FakeLoyaltyMetricRepository();
        var userRepository = new FakeUserRepository();
        var balanceRepository = new FakeMetricBalanceRepository();
        var transactionRepository = new FakeStampTransactionRepository();
        brandRepository.AddExisting(brand);
        metricRepository.AddExisting(metric);
        membershipRepository.SetRole(actorUserId, brand.Id, SystemRoles.Staff);

        var handler = new IssueMetricByPhoneHandler(
            new BrandAccessService(membershipRepository),
            brandRepository,
            new MetricLedgerService(balanceRepository, transactionRepository),
            metricRepository,
            CreatePhoneAccountService(userRepository));

        var result = await handler.Handle(
            new IssueMetricByPhoneCommand(
                metric.Id,
                actorUserId,
                new IssueMetricByPhoneRequest("+7 999 123-45-67", 3, "Visit")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var customer = Assert.Single(userRepository.Users);
        Assert.Equal(customer.Id, result.Value.UserId);
        Assert.Equal(3, result.Value.Amount);
        Assert.Equal(3, result.Value.BalanceValue);
        Assert.Single(balanceRepository.Balances);
        Assert.Single(transactionRepository.Transactions);
    }

    [Fact]
    public async Task Handle_WhenActorHasNoAccess_ShouldNotCreateUser()
    {
        var brand = Brand.Create("Coffee").Value;
        var metric = LoyaltyMetricDefinition.Create(brand.Id, "Coffee stamp", redemptionAmount: 6).Value;
        var brandRepository = new FakeBrandRepository();
        var metricRepository = new FakeLoyaltyMetricRepository();
        var userRepository = new FakeUserRepository();
        brandRepository.AddExisting(brand);
        metricRepository.AddExisting(metric);

        var handler = new IssueMetricByPhoneHandler(
            new BrandAccessService(new FakeBrandMembershipRepository()),
            brandRepository,
            new MetricLedgerService(new FakeMetricBalanceRepository(), new FakeStampTransactionRepository()),
            metricRepository,
            CreatePhoneAccountService(userRepository));

        var result = await handler.Handle(
            new IssueMetricByPhoneCommand(
                metric.Id,
                Guid.NewGuid(),
                new IssueMetricByPhoneRequest("+79991234567", 3, "Visit")),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Empty(userRepository.Users);
    }

    [Fact]
    public async Task Handle_WhenAmountIsInvalid_ShouldNotCreateUser()
    {
        var brand = Brand.Create("Coffee").Value;
        var metric = LoyaltyMetricDefinition.Create(brand.Id, "Coffee stamp", redemptionAmount: 6).Value;
        var actorUserId = Guid.NewGuid();
        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        var metricRepository = new FakeLoyaltyMetricRepository();
        var userRepository = new FakeUserRepository();
        brandRepository.AddExisting(brand);
        metricRepository.AddExisting(metric);
        membershipRepository.SetRole(actorUserId, brand.Id, SystemRoles.Staff);

        var handler = new IssueMetricByPhoneHandler(
            new BrandAccessService(membershipRepository),
            brandRepository,
            new MetricLedgerService(new FakeMetricBalanceRepository(), new FakeStampTransactionRepository()),
            metricRepository,
            CreatePhoneAccountService(userRepository));

        var result = await handler.Handle(
            new IssueMetricByPhoneCommand(
                metric.Id,
                actorUserId,
                new IssueMetricByPhoneRequest("+79991234567", 0, "Visit")),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Empty(userRepository.Users);
    }

    private static PhoneAccountService CreatePhoneAccountService(FakeUserRepository repository)
    {
        return new PhoneAccountService(
            repository,
            new FixedDisplayNameGenerator());
    }

    private sealed class FixedDisplayNameGenerator : IUserDisplayNameGenerator
    {
        public string Generate() => "Business customer";
    }
}
