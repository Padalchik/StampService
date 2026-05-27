using StampService.Application.Access;
using StampService.Application.CustomerNotifications;
using StampService.Application.Errors;
using StampService.Application.Metrics;
using StampService.Application.Metrics.Commands.RedeemMetric;
using StampService.Application.Users.Commands.UseRedemptionCode;
using StampService.ApplicationTests.Fakes;
using StampService.Contracts.DTOs.Coins;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Access;
using StampService.Domain.Brand;
using StampService.Domain.Loyalty;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Metrics;

public class RedeemMetricHandlerTests
{
    [Fact]
    public async Task Handle_WhenRedemptionCodeIsActive_ShouldConsumeCodeAndRedeemMetric()
    {
        var now = new DateTimeOffset(2026, 5, 7, 10, 0, 0, TimeSpan.Zero);
        var redeemerUserId = Guid.NewGuid();
        var customerUserId = Guid.NewGuid();
        var brand = Brand.Create("Coffee").Value;
        var brandId = brand.Id;
        var metric = LoyaltyMetricDefinition.Create(brandId, "Coffee", 3).Value;

        var metricRepository = new FakeLoyaltyMetricRepository();
        metricRepository.AddExisting(metric);
        var brandRepository = new FakeBrandRepository();
        brandRepository.AddExisting(brand);
        var membershipRepository = new FakeBrandMembershipRepository();
        membershipRepository.SetRole(redeemerUserId, brandId, SystemRoles.Staff);

        var balanceRepository = new FakeMetricBalanceRepository();
        var transactionRepository = new FakeStampTransactionRepository();
        var balance = MetricBalance.Create(customerUserId, brandId, metric.Id).Value;
        balanceRepository.Add(balance);
        transactionRepository.Add(StampTransaction.CreateIssue(balance.Id, 10, "Initial issue", redeemerUserId).Value);

        var codeRepository = new FakeRedemptionCodeRepository();
        var redemptionCode = RedemptionCode.Create(
            customerUserId,
            "1234",
            now.UtcDateTime.AddMinutes(5),
            now.UtcDateTime).Value;
        codeRepository.Add(redemptionCode);

        var useCodeHandler = new UseRedemptionCodeHandler(
            codeRepository,
            new FixedTimeProvider(now));
        var notificationService = new RecordingCustomerNotificationService();
        var validationService = new RedeemMetricValidationService(
            new BrandAccessService(membershipRepository),
            brandRepository,
            metricRepository,
            codeRepository,
            balanceRepository,
            transactionRepository,
            new FixedTimeProvider(now));

        var handler = new RedeemMetricHandler(
            new MetricLedgerService(balanceRepository, transactionRepository),
            validationService,
            useCodeHandler,
            notificationService);

        var result = await handler.Handle(
            new RedeemMetricCommand(
                metric.Id,
                redeemerUserId,
                new RedeemMetricRequest("1234", "Redeem")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(customerUserId, result.Value.UserId);
        Assert.Equal(3, result.Value.Amount);
        Assert.Equal(7, result.Value.BalanceValue);
        Assert.NotNull(redemptionCode.UsedAtUtc);
        Assert.Equal(result.Value, notificationService.MetricRedeemed);
    }

    [Fact]
    public async Task Handle_WhenRedemptionCodeIsAlreadyUsed_ShouldFail()
    {
        var now = new DateTimeOffset(2026, 5, 7, 10, 0, 0, TimeSpan.Zero);
        var redeemerUserId = Guid.NewGuid();
        var customerUserId = Guid.NewGuid();
        var brand = Brand.Create("Coffee").Value;
        var brandId = brand.Id;
        var metric = LoyaltyMetricDefinition.Create(brandId, "Coffee", 3).Value;

        var metricRepository = new FakeLoyaltyMetricRepository();
        metricRepository.AddExisting(metric);
        var brandRepository = new FakeBrandRepository();
        brandRepository.AddExisting(brand);
        var membershipRepository = new FakeBrandMembershipRepository();
        membershipRepository.SetRole(redeemerUserId, brandId, SystemRoles.Staff);

        var codeRepository = new FakeRedemptionCodeRepository();
        var redemptionCode = RedemptionCode.Create(
            customerUserId,
            "1234",
            now.UtcDateTime.AddMinutes(5),
            now.UtcDateTime).Value;
        redemptionCode.Use(now.UtcDateTime.AddMinutes(1));
        codeRepository.Add(redemptionCode);

        var balanceRepository = new FakeMetricBalanceRepository();
        var transactionRepository = new FakeStampTransactionRepository();
        var handler = new RedeemMetricHandler(
            new MetricLedgerService(balanceRepository, transactionRepository),
            new RedeemMetricValidationService(
                new BrandAccessService(membershipRepository),
                brandRepository,
                metricRepository,
                codeRepository,
                balanceRepository,
                transactionRepository,
                new FixedTimeProvider(now)),
            new UseRedemptionCodeHandler(codeRepository, new FixedTimeProvider(now)));

        var result = await handler.Handle(
            new RedeemMetricCommand(
                metric.Id,
                redeemerUserId,
                new RedeemMetricRequest("1234", "Redeem")),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task Handle_WhenBalanceIsInsufficient_ShouldFail()
    {
        var now = new DateTimeOffset(2026, 5, 7, 10, 0, 0, TimeSpan.Zero);
        var redeemerUserId = Guid.NewGuid();
        var customerUserId = Guid.NewGuid();
        var brand = Brand.Create("Coffee").Value;
        var brandId = brand.Id;
        var metric = LoyaltyMetricDefinition.Create(brandId, "Coffee", 5).Value;

        var metricRepository = new FakeLoyaltyMetricRepository();
        metricRepository.AddExisting(metric);
        var brandRepository = new FakeBrandRepository();
        brandRepository.AddExisting(brand);
        var membershipRepository = new FakeBrandMembershipRepository();
        membershipRepository.SetRole(redeemerUserId, brandId, SystemRoles.Staff);

        var balanceRepository = new FakeMetricBalanceRepository();
        var transactionRepository = new FakeStampTransactionRepository();
        var balance = MetricBalance.Create(customerUserId, brandId, metric.Id).Value;
        balanceRepository.Add(balance);
        transactionRepository.Add(StampTransaction.CreateIssue(balance.Id, 3, "Initial issue", redeemerUserId).Value);

        var codeRepository = new FakeRedemptionCodeRepository();
        codeRepository.Add(RedemptionCode.Create(
            customerUserId,
            "1234",
            now.UtcDateTime.AddMinutes(5),
            now.UtcDateTime).Value);

        var handler = new RedeemMetricHandler(
            new MetricLedgerService(balanceRepository, transactionRepository),
            new RedeemMetricValidationService(
                new BrandAccessService(membershipRepository),
                brandRepository,
                metricRepository,
                codeRepository,
                balanceRepository,
                transactionRepository,
                new FixedTimeProvider(now)),
            new UseRedemptionCodeHandler(codeRepository, new FixedTimeProvider(now)));

        var result = await handler.Handle(
            new RedeemMetricCommand(
                metric.Id,
                redeemerUserId,
                new RedeemMetricRequest("1234", "Redeem")),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task Handle_WhenMetricsAreDisabled_ShouldFail()
    {
        var now = new DateTimeOffset(2026, 5, 7, 10, 0, 0, TimeSpan.Zero);
        var redeemerUserId = Guid.NewGuid();
        var customerUserId = Guid.NewGuid();
        var brand = Brand.Create("Coffee").Value;
        brand.UpdateDetails("Coffee", isMetricsEnabled: false, isCoinsEnabled: true);
        var metric = LoyaltyMetricDefinition.Create(brand.Id, "Coffee", 3).Value;

        var metricRepository = new FakeLoyaltyMetricRepository();
        metricRepository.AddExisting(metric);
        var brandRepository = new FakeBrandRepository();
        brandRepository.AddExisting(brand);
        var membershipRepository = new FakeBrandMembershipRepository();
        membershipRepository.SetRole(redeemerUserId, brand.Id, SystemRoles.Staff);

        var codeRepository = new FakeRedemptionCodeRepository();
        codeRepository.Add(RedemptionCode.Create(
            customerUserId,
            "1234",
            now.UtcDateTime.AddMinutes(5),
            now.UtcDateTime).Value);

        var handler = new RedeemMetricHandler(
            new MetricLedgerService(new FakeMetricBalanceRepository(), new FakeStampTransactionRepository()),
            new RedeemMetricValidationService(
                new BrandAccessService(membershipRepository),
                brandRepository,
                metricRepository,
                codeRepository,
                new FakeMetricBalanceRepository(),
                new FakeStampTransactionRepository(),
                new FixedTimeProvider(now)),
            new UseRedemptionCodeHandler(codeRepository, new FixedTimeProvider(now)));

        var result = await handler.Handle(
            new RedeemMetricCommand(
                metric.Id,
                redeemerUserId,
                new RedeemMetricRequest("1234", "Redeem")),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(AppErrorCodes.Brand.MetricsDisabled, result.Errors[0].Metadata["error_code"]);
    }

    private sealed class RecordingCustomerNotificationService : ICustomerNotificationService
    {
        public RedeemMetricResponse? MetricRedeemed { get; private set; }

        public Task NotifyCoinsIssuedAsync(CoinOperationResponse operation, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task NotifyMetricIssuedAsync(IssueMetricResponse operation, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task NotifyCoinsRedeemedAsync(
            CoinOperationResponse operation,
            string comment,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task NotifyCoinProductPurchasedAsync(
            CoinOperationResponse operation,
            string productName,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task NotifyMetricRedeemedAsync(
            RedeemMetricResponse operation,
            CancellationToken cancellationToken)
        {
            MetricRedeemed = operation;
            return Task.CompletedTask;
        }
    }
}
