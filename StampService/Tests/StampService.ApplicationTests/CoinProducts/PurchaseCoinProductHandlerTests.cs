using StampService.Application.Access;
using StampService.Application.CoinProducts.Commands.PurchaseCoinProduct;
using StampService.Application.Coins;
using StampService.Application.Users.Commands.UseRedemptionCode;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Access;
using StampService.Domain.Coins;
using StampService.Domain.User;

namespace StampService.ApplicationTests.CoinProducts;

public class PurchaseCoinProductHandlerTests
{
    [Fact]
    public async Task Handle_WhenCodeIsActiveAndBalanceIsEnough_ShouldConsumeCodeAndRedeemProductPrice()
    {
        var fixture = CreateFixture(productPrice: 7, balance: 10);

        var result = await fixture.Handler.Handle(
            new PurchaseCoinProductCommand(
                fixture.BrandId,
                fixture.StaffUserId,
                "1234",
                fixture.Product.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(fixture.Customer.Id, result.Value.UserId);
        Assert.Equal(7, result.Value.Amount);
        Assert.Equal(3, result.Value.BalanceValue);
        Assert.Equal("Redeem", result.Value.TransactionType);
        Assert.NotNull(fixture.RedemptionCode.UsedAtUtc);

        var redeemTransaction = fixture.TransactionRepository.Transactions
            .Single(transaction => transaction.Type == CoinTransactionType.Redeem);
        Assert.Equal("Coffee", redeemTransaction.Comment);
        Assert.Equal(fixture.StaffUserId, redeemTransaction.ActorUserId);
    }

    [Fact]
    public async Task Handle_WhenBalanceIsInsufficient_ShouldFailWithoutConsumingCodeOrCreatingRedeemTransaction()
    {
        var fixture = CreateFixture(productPrice: 7, balance: 5);

        var result = await fixture.Handler.Handle(
            new PurchaseCoinProductCommand(
                fixture.BrandId,
                fixture.StaffUserId,
                "1234",
                fixture.Product.Id),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Null(fixture.RedemptionCode.UsedAtUtc);
        Assert.Single(fixture.TransactionRepository.Transactions);
        Assert.Equal(5, fixture.Wallet.Value);
    }

    [Fact]
    public async Task Handle_WhenProductIsInactive_ShouldFailWithoutConsumingCode()
    {
        var fixture = CreateFixture(productPrice: 7, balance: 10);
        fixture.Product.Deactivate();

        var result = await fixture.Handler.Handle(
            new PurchaseCoinProductCommand(
                fixture.BrandId,
                fixture.StaffUserId,
                "1234",
                fixture.Product.Id),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Null(fixture.RedemptionCode.UsedAtUtc);
        Assert.Single(fixture.TransactionRepository.Transactions);
    }

    [Fact]
    public async Task Handle_WhenActorCannotRedeem_ShouldFail()
    {
        var fixture = CreateFixture(productPrice: 7, balance: 10, grantAccess: false);

        var result = await fixture.Handler.Handle(
            new PurchaseCoinProductCommand(
                fixture.BrandId,
                fixture.StaffUserId,
                "1234",
                fixture.Product.Id),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Null(fixture.RedemptionCode.UsedAtUtc);
    }

    private static Fixture CreateFixture(int productPrice, int balance, bool grantAccess = true)
    {
        var now = new DateTimeOffset(2026, 5, 13, 10, 0, 0, TimeSpan.Zero);
        var brandId = Guid.NewGuid();
        var staffUserId = Guid.NewGuid();
        var customer = User.Create("Customer", "1234").Value;
        var product = CoinProduct.Create(brandId, "Coffee", productPrice).Value;

        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        var productRepository = new FakeCoinProductRepository();
        var walletRepository = new FakeCoinWalletRepository();
        var transactionRepository = new FakeCoinTransactionRepository();
        var codeRepository = new FakeRedemptionCodeRepository();
        var userRepository = new FakeUserRepository();

        brandRepository.AddExisting(brandId);
        if (grantAccess)
            membershipRepository.SetRole(staffUserId, brandId, SystemRoles.Staff);

        userRepository.Add(customer);
        productRepository.Add(product);

        var wallet = CoinWallet.Create(customer.Id, brandId).Value;
        wallet.SetMaterializedValue(balance);
        walletRepository.Add(wallet);
        transactionRepository.Add(CoinTransaction.CreateIssue(wallet.Id, balance, "Initial issue", staffUserId).Value);

        var redemptionCode = RedemptionCode.Create(
            customer.Id,
            "1234",
            now.UtcDateTime.AddMinutes(3),
            now.UtcDateTime).Value;
        codeRepository.Add(redemptionCode);

        var handler = new PurchaseCoinProductHandler(
            new BrandAccessService(membershipRepository),
            brandRepository,
            new CoinLedgerService(walletRepository, transactionRepository),
            productRepository,
            transactionRepository,
            walletRepository,
            codeRepository,
            userRepository,
            new UseRedemptionCodeHandler(codeRepository, new FixedTimeProvider(now)),
            new FixedTimeProvider(now));

        return new Fixture(
            handler,
            brandId,
            staffUserId,
            customer,
            product,
            wallet,
            redemptionCode,
            transactionRepository);
    }

    private sealed record Fixture(
        PurchaseCoinProductHandler Handler,
        Guid BrandId,
        Guid StaffUserId,
        User Customer,
        CoinProduct Product,
        CoinWallet Wallet,
        RedemptionCode RedemptionCode,
        FakeCoinTransactionRepository TransactionRepository);
}
