using StampService.Application.Access;
using StampService.Application.CustomerNotifications;
using StampService.Application.Metrics;
using StampService.Application.Metrics.Commands.IssueMetric;
using StampService.Application.Users;
using StampService.ApplicationTests.Fakes;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Access;
using StampService.Domain.Brand;
using StampService.Domain.Loyalty;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Metrics;

public class IssueMetricByPhoneHandlerTests
{
    [Fact]
    public async Task Handle_WhenPhoneUserExists_ShouldIssueMetric()
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
        var notificationService = new RecordingCustomerNotificationService();
        var customer = CreatePhoneUser("+79991234567");
        brandRepository.AddExisting(brand);
        metricRepository.AddExisting(metric);
        userRepository.Add(customer);
        membershipRepository.SetRole(actorUserId, brand.Id, SystemRoles.Staff);

        var handler = new IssueMetricByPhoneHandler(
            new BrandAccessService(membershipRepository),
            brandRepository,
            new MetricLedgerService(balanceRepository, transactionRepository),
            metricRepository,
            CreatePhoneAccountService(userRepository),
            notificationService);

        var result = await handler.Handle(
            new IssueMetricByPhoneCommand(
                metric.Id,
                actorUserId,
                new IssueMetricByPhoneRequest("+7 999 123-45-67", 3, "Visit")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(userRepository.Users);
        Assert.Equal(customer.Id, result.Value.UserId);
        Assert.Equal(3, result.Value.Amount);
        Assert.Equal(3, result.Value.BalanceValue);
        Assert.Single(balanceRepository.Balances);
        Assert.Single(transactionRepository.Transactions);
        Assert.Equal(result.Value, notificationService.MetricIssued);
    }

    [Fact]
    public async Task Handle_WhenPhoneUserDoesNotExist_ShouldFailWithoutCreatingUser()
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

        Assert.True(result.IsFailed);
        Assert.Empty(userRepository.Users);
        Assert.Empty(balanceRepository.Balances);
        Assert.Empty(transactionRepository.Transactions);
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

    private static User CreatePhoneUser(string phoneNumber)
    {
        var user = User.Create("Business customer").Value;
        user.AddIdentity(IdentityType.Phone, phoneNumber, "{}");
        return user;
    }

    private sealed class FixedDisplayNameGenerator : IUserDisplayNameGenerator
    {
        public string Generate() => "Business customer";
    }

    private sealed class RecordingCustomerNotificationService : ICustomerNotificationService
    {
        public IssueMetricResponse? MetricIssued { get; private set; }

        public Task NotifyCoinsIssuedAsync(
            StampService.Contracts.DTOs.Coins.CoinOperationResponse operation,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task NotifyMetricIssuedAsync(
            IssueMetricResponse operation,
            CancellationToken cancellationToken)
        {
            MetricIssued = operation;
            return Task.CompletedTask;
        }

        public Task NotifyCoinsRedeemedAsync(
            StampService.Contracts.DTOs.Coins.CoinOperationResponse operation,
            string comment,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task NotifyCoinProductPurchasedAsync(
            StampService.Contracts.DTOs.Coins.CoinOperationResponse operation,
            string productName,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task NotifyMetricRedeemedAsync(
            RedeemMetricResponse operation,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
