using StampService.Application.Access;
using StampService.Application.CoinProducts.Queries.GetCoinProductPurchaseOptions;
using StampService.Application.Errors;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Access;
using StampService.Domain.Brand;
using StampService.Domain.Coins;
using StampService.Domain.User;

namespace StampService.ApplicationTests.CoinProducts;

public class GetCoinProductPurchaseOptionsHandlerTests
{
    [Fact]
    public async Task Handle_WhenCodeIsActive_ShouldReturnProductsWithPurchaseAvailability()
    {
        var now = new DateTimeOffset(2026, 5, 13, 10, 0, 0, TimeSpan.Zero);
        var brand = Brand.Create("Coffee").Value;
        var brandId = brand.Id;
        var staffUserId = Guid.NewGuid();
        var customer = User.Create("Customer", "1234").Value;
        var availableProduct = CoinProduct.Create(brandId, "Coffee", 7).Value;
        var unavailableProduct = CoinProduct.Create(brandId, "Cake", 12).Value;
        var inactiveProduct = CoinProduct.Create(brandId, "Tea", 1).Value;
        inactiveProduct.Deactivate();

        var membershipRepository = new FakeBrandMembershipRepository();
        var brandRepository = new FakeBrandRepository();
        var productRepository = new FakeCoinProductRepository();
        var walletRepository = new FakeCoinWalletRepository();
        var transactionRepository = new FakeCoinTransactionRepository();
        var codeRepository = new FakeRedemptionCodeRepository();
        var userRepository = new FakeUserRepository();

        membershipRepository.SetRole(staffUserId, brandId, SystemRoles.Staff);
        brandRepository.AddExisting(brand);
        userRepository.Add(customer);
        productRepository.Add(availableProduct);
        productRepository.Add(unavailableProduct);
        productRepository.Add(inactiveProduct);

        var wallet = CoinWallet.Create(customer.Id, brandId).Value;
        walletRepository.Add(wallet);
        transactionRepository.Add(CoinTransaction.CreateIssue(wallet.Id, 10, "Initial issue", staffUserId).Value);

        codeRepository.Add(RedemptionCode.Create(
            customer.Id,
            "1234",
            now.UtcDateTime.AddMinutes(5),
            now.UtcDateTime).Value);

        var handler = new GetCoinProductPurchaseOptionsHandler(
            new BrandAccessService(membershipRepository),
            brandRepository,
            productRepository,
            transactionRepository,
            walletRepository,
            codeRepository,
            userRepository,
            new FixedTimeProvider(now));

        var result = await handler.Handle(
            new GetCoinProductPurchaseOptionsQuery(staffUserId, brandId, "1234"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(customer.Id, result.Value.CustomerUserId);
        Assert.Equal("1234", result.Value.RedemptionCode);
        Assert.Equal(2, result.Value.Products.Count);

        var available = result.Value.Products.Single(product => product.ProductId == availableProduct.Id);
        Assert.True(available.CanPurchase);
        Assert.Equal(10, available.CurrentBalance);
        Assert.Equal(7, available.Price);

        var unavailable = result.Value.Products.Single(product => product.ProductId == unavailableProduct.Id);
        Assert.False(unavailable.CanPurchase);
        Assert.Equal(10, unavailable.CurrentBalance);
        Assert.Equal(12, unavailable.Price);

        Assert.DoesNotContain(result.Value.Products, product => product.ProductId == inactiveProduct.Id);
    }

    [Fact]
    public async Task Handle_WhenWalletDoesNotExist_ShouldReturnZeroBalanceOptions()
    {
        var now = new DateTimeOffset(2026, 5, 13, 10, 0, 0, TimeSpan.Zero);
        var brand = Brand.Create("Coffee").Value;
        var brandId = brand.Id;
        var staffUserId = Guid.NewGuid();
        var customer = User.Create("Customer", "1234").Value;
        var product = CoinProduct.Create(brandId, "Coffee", 7).Value;

        var membershipRepository = new FakeBrandMembershipRepository();
        var brandRepository = new FakeBrandRepository();
        var productRepository = new FakeCoinProductRepository();
        var codeRepository = new FakeRedemptionCodeRepository();
        var userRepository = new FakeUserRepository();

        membershipRepository.SetRole(staffUserId, brandId, SystemRoles.Staff);
        brandRepository.AddExisting(brand);
        productRepository.Add(product);
        userRepository.Add(customer);
        codeRepository.Add(RedemptionCode.Create(
            customer.Id,
            "1234",
            now.UtcDateTime.AddMinutes(5),
            now.UtcDateTime).Value);

        var handler = new GetCoinProductPurchaseOptionsHandler(
            new BrandAccessService(membershipRepository),
            brandRepository,
            productRepository,
            new FakeCoinTransactionRepository(),
            new FakeCoinWalletRepository(),
            codeRepository,
            userRepository,
            new FixedTimeProvider(now));

        var result = await handler.Handle(
            new GetCoinProductPurchaseOptionsQuery(staffUserId, brandId, "1234"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var option = Assert.Single(result.Value.Products);
        Assert.False(option.CanPurchase);
        Assert.Equal(0, option.CurrentBalance);
    }

    [Fact]
    public async Task Handle_WhenCoinProductRedemptionIsDisabled_ShouldFail()
    {
        var now = new DateTimeOffset(2026, 5, 13, 10, 0, 0, TimeSpan.Zero);
        var brand = Brand.Create("Coffee").Value;
        brand.UpdateDetails(
            "Coffee",
            isMetricsEnabled: true,
            isCoinsEnabled: true,
            isCoinProductRedemptionEnabled: false,
            isManualCoinRedemptionEnabled: true);
        var staffUserId = Guid.NewGuid();
        var customer = User.Create("Customer", "1234").Value;

        var membershipRepository = new FakeBrandMembershipRepository();
        var brandRepository = new FakeBrandRepository();
        var codeRepository = new FakeRedemptionCodeRepository();
        var userRepository = new FakeUserRepository();

        membershipRepository.SetRole(staffUserId, brand.Id, SystemRoles.Staff);
        brandRepository.AddExisting(brand);
        userRepository.Add(customer);
        codeRepository.Add(RedemptionCode.Create(
            customer.Id,
            "1234",
            now.UtcDateTime.AddMinutes(5),
            now.UtcDateTime).Value);

        var handler = new GetCoinProductPurchaseOptionsHandler(
            new BrandAccessService(membershipRepository),
            brandRepository,
            new FakeCoinProductRepository(),
            new FakeCoinTransactionRepository(),
            new FakeCoinWalletRepository(),
            codeRepository,
            userRepository,
            new FixedTimeProvider(now));

        var result = await handler.Handle(
            new GetCoinProductPurchaseOptionsQuery(staffUserId, brand.Id, "1234"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(AppErrorCodes.Brand.CoinProductRedemptionDisabled, result.Errors[0].Metadata["error_code"]);
    }

    [Fact]
    public async Task Handle_WhenActorCannotRedeem_ShouldFail()
    {
        var handler = new GetCoinProductPurchaseOptionsHandler(
            new BrandAccessService(new FakeBrandMembershipRepository()),
            new FakeBrandRepository(),
            new FakeCoinProductRepository(),
            new FakeCoinTransactionRepository(),
            new FakeCoinWalletRepository(),
            new FakeRedemptionCodeRepository(),
            new FakeUserRepository(),
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        var result = await handler.Handle(
            new GetCoinProductPurchaseOptionsQuery(Guid.NewGuid(), Guid.NewGuid(), "1234"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }
}
