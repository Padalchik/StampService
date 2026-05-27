using StampService.Application.Access;
using StampService.Application.Coins;
using StampService.Application.Coins.Commands.RedeemCoins;
using StampService.Application.CustomerNotifications;
using StampService.Application.Errors;
using StampService.Application.Users.Commands.UseRedemptionCode;
using StampService.ApplicationTests.Fakes;
using StampService.Contracts.DTOs.Coins;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Access;
using StampService.Domain.Brand;
using StampService.Domain.Coins;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Coins;

public class RedeemCoinsHandlerTests
{
    [Fact]
    public async Task Handle_WhenManualRedemptionIsEnabled_ShouldConsumeCodeAndRedeemCoins()
    {
        var fixture = CreateFixture(balance: 10, manualRedemptionEnabled: true);

        var result = await fixture.Handler.Handle(
            new RedeemCoinsCommand(
                fixture.BrandId,
                fixture.StaffUserId,
                "1234",
                Amount: 6,
                Comment: "Receipt"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(fixture.Customer.Id, result.Value.UserId);
        Assert.Equal(6, result.Value.Amount);
        Assert.Equal(4, result.Value.BalanceValue);
        Assert.NotNull(fixture.RedemptionCode.UsedAtUtc);
        Assert.Equal(result.Value, fixture.NotificationService.CoinsRedeemed);
        Assert.Equal("Receipt", fixture.NotificationService.CoinsRedeemedComment);

        var redeemTransaction = fixture.TransactionRepository.Transactions
            .Single(transaction => transaction.Type == CoinTransactionType.Redeem);
        Assert.Equal("Receipt", redeemTransaction.Comment);
    }

    [Fact]
    public async Task Handle_WhenManualRedemptionIsDisabled_ShouldFailWithoutConsumingCode()
    {
        var fixture = CreateFixture(balance: 10, manualRedemptionEnabled: false);

        var result = await fixture.Handler.Handle(
            new RedeemCoinsCommand(
                fixture.BrandId,
                fixture.StaffUserId,
                "1234",
                Amount: 6,
                Comment: "Receipt"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(AppErrorCodes.Brand.ManualCoinRedemptionDisabled, result.Errors[0].Metadata["error_code"]);
        Assert.Null(fixture.RedemptionCode.UsedAtUtc);
    }

    [Fact]
    public async Task Handle_WhenBalanceIsInsufficient_ShouldFailWithoutConsumingCode()
    {
        var fixture = CreateFixture(balance: 5, manualRedemptionEnabled: true);

        var result = await fixture.Handler.Handle(
            new RedeemCoinsCommand(
                fixture.BrandId,
                fixture.StaffUserId,
                "1234",
                Amount: 6,
                Comment: "Receipt"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Null(fixture.RedemptionCode.UsedAtUtc);
    }

    [Fact]
    public async Task Handle_WhenActorHasNoRedeemPermission_ShouldFailWithoutConsumingCode()
    {
        var fixture = CreateFixture(balance: 10, manualRedemptionEnabled: true, grantAccess: false);

        var result = await fixture.Handler.Handle(
            new RedeemCoinsCommand(
                fixture.BrandId,
                fixture.StaffUserId,
                "1234",
                Amount: 6,
                Comment: "Receipt"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(AppErrorCodes.Access.Denied, result.Errors[0].Metadata["error_code"]);
        Assert.Null(fixture.RedemptionCode.UsedAtUtc);
        Assert.DoesNotContain(fixture.TransactionRepository.Transactions, transaction => transaction.Type == CoinTransactionType.Redeem);
    }

    [Fact]
    public async Task Handle_WhenRedemptionCodeFormatIsInvalid_ShouldFailWithoutConsumingCode()
    {
        var fixture = CreateFixture(balance: 10, manualRedemptionEnabled: true);

        var result = await fixture.Handler.Handle(
            new RedeemCoinsCommand(
                fixture.BrandId,
                fixture.StaffUserId,
                "12A4",
                Amount: 6,
                Comment: "Receipt"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(AppErrorCodes.RedemptionCode.Invalid, result.Errors[0].Metadata["error_code"]);
        Assert.Null(fixture.RedemptionCode.UsedAtUtc);
        Assert.DoesNotContain(fixture.TransactionRepository.Transactions, transaction => transaction.Type == CoinTransactionType.Redeem);
    }

    [Fact]
    public async Task Handle_WhenRedemptionCodeIsExpired_ShouldFailWithoutConsumingCode()
    {
        var fixture = CreateFixture(
            balance: 10,
            manualRedemptionEnabled: true,
            codeExpiresAtUtc: new DateTime(2026, 5, 14, 9, 59, 0, DateTimeKind.Utc),
            codeCreatedAtUtc: new DateTime(2026, 5, 14, 9, 54, 0, DateTimeKind.Utc));

        var result = await fixture.Handler.Handle(
            new RedeemCoinsCommand(
                fixture.BrandId,
                fixture.StaffUserId,
                "1234",
                Amount: 6,
                Comment: "Receipt"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(AppErrorCodes.RedemptionCode.NotFoundOrExpired, result.Errors[0].Metadata["error_code"]);
        Assert.Null(fixture.RedemptionCode.UsedAtUtc);
        Assert.DoesNotContain(fixture.TransactionRepository.Transactions, transaction => transaction.Type == CoinTransactionType.Redeem);
    }

    [Fact]
    public async Task Handle_WhenCoinsAreDisabled_ShouldFailWithoutConsumingCode()
    {
        var fixture = CreateFixture(balance: 10, manualRedemptionEnabled: true, coinsEnabled: false);

        var result = await fixture.Handler.Handle(
            new RedeemCoinsCommand(
                fixture.BrandId,
                fixture.StaffUserId,
                "1234",
                Amount: 6,
                Comment: "Receipt"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(AppErrorCodes.Brand.CoinsDisabled, result.Errors[0].Metadata["error_code"]);
        Assert.Null(fixture.RedemptionCode.UsedAtUtc);
        Assert.DoesNotContain(fixture.TransactionRepository.Transactions, transaction => transaction.Type == CoinTransactionType.Redeem);
    }

    [Fact]
    public async Task Handle_WhenCommentIsBlank_ShouldUseDefaultComment()
    {
        var fixture = CreateFixture(balance: 10, manualRedemptionEnabled: true);

        var result = await fixture.Handler.Handle(
            new RedeemCoinsCommand(
                fixture.BrandId,
                fixture.StaffUserId,
                "1234",
                Amount: 6,
                Comment: "  "),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var redeemTransaction = fixture.TransactionRepository.Transactions
            .Single(transaction => transaction.Type == CoinTransactionType.Redeem);
        Assert.Equal("Manual coin redemption", redeemTransaction.Comment);
    }

    private static Fixture CreateFixture(
        int balance,
        bool manualRedemptionEnabled,
        bool grantAccess = true,
        bool coinsEnabled = true,
        DateTime? codeExpiresAtUtc = null,
        DateTime? codeCreatedAtUtc = null)
    {
        var now = new DateTimeOffset(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        var brand = Brand.Create("Coffee").Value;
        brand.UpdateDetails(
            "Coffee",
            isMetricsEnabled: true,
            isCoinsEnabled: coinsEnabled,
            isCoinProductRedemptionEnabled: coinsEnabled && !manualRedemptionEnabled,
            isManualCoinRedemptionEnabled: manualRedemptionEnabled);
        var staffUserId = Guid.NewGuid();
        var customer = User.Create("Customer").Value;

        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        var walletRepository = new FakeCoinWalletRepository();
        var transactionRepository = new FakeCoinTransactionRepository();
        var codeRepository = new FakeRedemptionCodeRepository();
        var userRepository = new FakeUserRepository();
        var notificationService = new RecordingCustomerNotificationService();

        brandRepository.AddExisting(brand);
        if (grantAccess)
            membershipRepository.SetRole(staffUserId, brand.Id, SystemRoles.Staff);
        userRepository.Add(customer);

        var wallet = CoinWallet.Create(customer.Id, brand.Id).Value;
        wallet.SetMaterializedValue(balance);
        walletRepository.Add(wallet);
        transactionRepository.Add(CoinTransaction.CreateIssue(wallet.Id, balance, "Initial issue", staffUserId).Value);

        var redemptionCode = RedemptionCode.Create(
            customer.Id,
            "1234",
            codeExpiresAtUtc ?? now.UtcDateTime.AddMinutes(5),
            codeCreatedAtUtc ?? now.UtcDateTime).Value;
        codeRepository.Add(redemptionCode);

        var handler = new RedeemCoinsHandler(
            new BrandAccessService(membershipRepository),
            brandRepository,
            new CoinLedgerService(walletRepository, transactionRepository),
            transactionRepository,
            walletRepository,
            codeRepository,
            userRepository,
            new UseRedemptionCodeHandler(codeRepository, new FixedTimeProvider(now)),
            new FixedTimeProvider(now),
            notificationService);

        return new Fixture(
            handler,
            brand.Id,
            staffUserId,
            customer,
            redemptionCode,
            transactionRepository,
            notificationService);
    }

    private sealed record Fixture(
        RedeemCoinsHandler Handler,
        Guid BrandId,
        Guid StaffUserId,
        User Customer,
        RedemptionCode RedemptionCode,
        FakeCoinTransactionRepository TransactionRepository,
        RecordingCustomerNotificationService NotificationService);

    private sealed class RecordingCustomerNotificationService : ICustomerNotificationService
    {
        public CoinOperationResponse? CoinsRedeemed { get; private set; }

        public string? CoinsRedeemedComment { get; private set; }

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
            CoinsRedeemed = operation;
            CoinsRedeemedComment = comment;
            return Task.CompletedTask;
        }

        public Task NotifyCoinProductPurchasedAsync(
            CoinOperationResponse operation,
            string productName,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task NotifyMetricRedeemedAsync(RedeemMetricResponse operation, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
