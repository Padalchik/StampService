using StampService.Application.Access;
using StampService.Application.Coins;
using StampService.Application.Coins.Commands.RedeemCoins;
using StampService.Application.Errors;
using StampService.Application.Users.Commands.UseRedemptionCode;
using StampService.ApplicationTests.Fakes;
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

    private static Fixture CreateFixture(int balance, bool manualRedemptionEnabled)
    {
        var now = new DateTimeOffset(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        var brand = Brand.Create("Coffee").Value;
        brand.UpdateDetails(
            "Coffee",
            isMetricsEnabled: true,
            isCoinsEnabled: true,
            isCoinProductRedemptionEnabled: !manualRedemptionEnabled,
            isManualCoinRedemptionEnabled: manualRedemptionEnabled);
        var staffUserId = Guid.NewGuid();
        var customer = User.Create("Customer", "1234").Value;

        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        var walletRepository = new FakeCoinWalletRepository();
        var transactionRepository = new FakeCoinTransactionRepository();
        var codeRepository = new FakeRedemptionCodeRepository();
        var userRepository = new FakeUserRepository();

        brandRepository.AddExisting(brand);
        membershipRepository.SetRole(staffUserId, brand.Id, SystemRoles.Staff);
        userRepository.Add(customer);

        var wallet = CoinWallet.Create(customer.Id, brand.Id).Value;
        wallet.SetMaterializedValue(balance);
        walletRepository.Add(wallet);
        transactionRepository.Add(CoinTransaction.CreateIssue(wallet.Id, balance, "Initial issue", staffUserId).Value);

        var redemptionCode = RedemptionCode.Create(
            customer.Id,
            "1234",
            now.UtcDateTime.AddMinutes(3),
            now.UtcDateTime).Value;
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
            new FixedTimeProvider(now));

        return new Fixture(
            handler,
            brand.Id,
            staffUserId,
            customer,
            redemptionCode,
            transactionRepository);
    }

    private sealed record Fixture(
        RedeemCoinsHandler Handler,
        Guid BrandId,
        Guid StaffUserId,
        User Customer,
        RedemptionCode RedemptionCode,
        FakeCoinTransactionRepository TransactionRepository);
}
