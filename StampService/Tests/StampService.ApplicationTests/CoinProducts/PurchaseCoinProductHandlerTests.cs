using StampService.Application.Access;
using StampService.Application.CoinProducts.Commands.PurchaseCoinProduct;
using StampService.Application.Coins;
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
        Assert.Equal(result.Value, fixture.NotificationService.CoinProductPurchased);
        Assert.Equal(fixture.Product.Name, fixture.NotificationService.CoinProductPurchasedName);

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

    [Fact]
    public async Task Handle_WhenCoinsAreDisabled_ShouldFailWithoutConsumingCode()
    {
        var fixture = CreateFixture(productPrice: 7, balance: 10, coinsEnabled: false);

        var result = await fixture.Handler.Handle(
            new PurchaseCoinProductCommand(
                fixture.BrandId,
                fixture.StaffUserId,
                "1234",
                fixture.Product.Id),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(AppErrorCodes.Brand.CoinsDisabled, result.Errors[0].Metadata["error_code"]);
        Assert.Null(fixture.RedemptionCode.UsedAtUtc);
    }

    [Fact]
    public async Task Handle_WhenCoinProductRedemptionIsDisabled_ShouldFailWithoutConsumingCode()
    {
        var fixture = CreateFixture(productPrice: 7, balance: 10, coinProductRedemptionEnabled: false);

        var result = await fixture.Handler.Handle(
            new PurchaseCoinProductCommand(
                fixture.BrandId,
                fixture.StaffUserId,
                "1234",
                fixture.Product.Id),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(AppErrorCodes.Brand.CoinProductRedemptionDisabled, result.Errors[0].Metadata["error_code"]);
        Assert.Null(fixture.RedemptionCode.UsedAtUtc);
    }

    private static Fixture CreateFixture(
        int productPrice,
        int balance,
        bool grantAccess = true,
        bool coinsEnabled = true,
        bool coinProductRedemptionEnabled = true)
    {
        var now = new DateTimeOffset(2026, 5, 13, 10, 0, 0, TimeSpan.Zero);
        var brand = Brand.Create("Coffee").Value;
        brand.UpdateDetails(
            "Coffee",
            isMetricsEnabled: true,
            isCoinsEnabled: coinsEnabled,
            isCoinProductRedemptionEnabled: coinProductRedemptionEnabled,
            isManualCoinRedemptionEnabled: !coinProductRedemptionEnabled);
        var brandId = brand.Id;
        var staffUserId = Guid.NewGuid();
        var customer = User.Create("Customer").Value;
        var product = CoinProduct.Create(brandId, "Coffee", productPrice).Value;

        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        var productRepository = new FakeCoinProductRepository();
        var walletRepository = new FakeCoinWalletRepository();
        var transactionRepository = new FakeCoinTransactionRepository();
        var codeRepository = new FakeRedemptionCodeRepository();
        var userRepository = new FakeUserRepository();
        var brandCustomerRepository = new FakeBrandCustomerRepository(userRepository);
        var notificationService = new RecordingCustomerNotificationService();

        brandRepository.AddExisting(brand);
        if (grantAccess)
            membershipRepository.SetRole(staffUserId, brandId, SystemRoles.Staff);

        userRepository.Add(customer);
        brandCustomerRepository.AddExisting(brandId, customer.Id, staffUserId);
        productRepository.Add(product);

        var wallet = CoinWallet.Create(customer.Id, brandId).Value;
        wallet.SetMaterializedValue(balance);
        walletRepository.Add(wallet);
        transactionRepository.Add(CoinTransaction.CreateIssue(wallet.Id, balance, "Initial issue", staffUserId).Value);

        var redemptionCode = RedemptionCode.Create(
            customer.Id,
            "1234",
            now.UtcDateTime.AddMinutes(5),
            now.UtcDateTime).Value;
        codeRepository.Add(redemptionCode);

        var handler = new PurchaseCoinProductHandler(
            new BrandAccessService(membershipRepository),
            brandCustomerRepository,
            brandRepository,
            new CoinLedgerService(walletRepository, transactionRepository),
            productRepository,
            transactionRepository,
            walletRepository,
            codeRepository,
            userRepository,
            new UseRedemptionCodeHandler(codeRepository, new FixedTimeProvider(now)),
            new FixedTimeProvider(now),
            notificationService);

        return new Fixture(
            handler,
            brandId,
            staffUserId,
            customer,
            product,
            wallet,
            redemptionCode,
            transactionRepository,
            notificationService);
    }

    private sealed record Fixture(
        PurchaseCoinProductHandler Handler,
        Guid BrandId,
        Guid StaffUserId,
        User Customer,
        CoinProduct Product,
        CoinWallet Wallet,
        RedemptionCode RedemptionCode,
        FakeCoinTransactionRepository TransactionRepository,
        RecordingCustomerNotificationService NotificationService);

    private sealed class RecordingCustomerNotificationService : ICustomerNotificationService
    {
        public CoinOperationResponse? CoinProductPurchased { get; private set; }

        public string? CoinProductPurchasedName { get; private set; }

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
            CoinProductPurchased = operation;
            CoinProductPurchasedName = productName;
            return Task.CompletedTask;
        }

        public Task NotifyMetricRedeemedAsync(RedeemMetricResponse operation, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
