using StampService.Application.Access;
using StampService.Application.Metrics;
using StampService.Application.Metrics.Commands.RedeemMetric;
using StampService.Application.Users.Commands.UseRedemptionCode;
using StampService.ApplicationTests.Fakes;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Access;
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
        var brandId = Guid.NewGuid();
        var metric = LoyaltyMetricDefinition.Create(brandId, "coffee", "Coffee", 3).Value;

        var metricRepository = new FakeLoyaltyMetricRepository();
        metricRepository.AddExisting(metric);
        var brandRepository = new FakeBrandRepository();
        brandRepository.AddExisting(brandId);
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
            now.UtcDateTime.AddMinutes(3),
            now.UtcDateTime).Value;
        codeRepository.Add(redemptionCode);

        var useCodeHandler = new UseRedemptionCodeHandler(
            codeRepository,
            new FixedTimeProvider(now));
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
            useCodeHandler);

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
    }

    [Fact]
    public async Task Handle_WhenRedemptionCodeIsAlreadyUsed_ShouldFail()
    {
        var now = new DateTimeOffset(2026, 5, 7, 10, 0, 0, TimeSpan.Zero);
        var redeemerUserId = Guid.NewGuid();
        var customerUserId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var metric = LoyaltyMetricDefinition.Create(brandId, "coffee", "Coffee", 3).Value;

        var metricRepository = new FakeLoyaltyMetricRepository();
        metricRepository.AddExisting(metric);
        var brandRepository = new FakeBrandRepository();
        brandRepository.AddExisting(brandId);
        var membershipRepository = new FakeBrandMembershipRepository();
        membershipRepository.SetRole(redeemerUserId, brandId, SystemRoles.Staff);

        var codeRepository = new FakeRedemptionCodeRepository();
        var redemptionCode = RedemptionCode.Create(
            customerUserId,
            "1234",
            now.UtcDateTime.AddMinutes(3),
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
        var brandId = Guid.NewGuid();
        var metric = LoyaltyMetricDefinition.Create(brandId, "coffee", "Coffee", 5).Value;

        var metricRepository = new FakeLoyaltyMetricRepository();
        metricRepository.AddExisting(metric);
        var brandRepository = new FakeBrandRepository();
        brandRepository.AddExisting(brandId);
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
            now.UtcDateTime.AddMinutes(3),
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
}
