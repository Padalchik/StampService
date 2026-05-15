using StampService.Application.Wallet.Queries.GetUserBrandRewards;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Brand;
using StampService.Domain.Coins;
using StampService.Domain.Loyalty;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Wallet;

public class GetUserBrandRewardsHandlerTests
{
    [Fact]
    public async Task Handle_WhenBrandHasProductsAndMetricBalances_ShouldReturnAvailability()
    {
        var user = User.Create("Customer", "1234").Value;
        var brand = Brand.Create("Brand").Value;
        var brandId = brand.Id;
        var otherBrandId = Guid.NewGuid();
        var userRepository = new FakeUserRepository();
        var productRepository = new FakeCoinProductRepository();
        var walletRepository = new FakeCoinWalletRepository();
        var brandRepository = new FakeBrandRepository();
        var metricBalanceRepository = new FakeMetricBalanceRepository();
        userRepository.Add(user);
        brandRepository.AddExisting(brand);

        var wallet = CoinWallet.Create(user.Id, brandId).Value;
        wallet.SetMaterializedValue(8);
        walletRepository.Add(wallet);

        var availableProduct = CoinProduct.Create(brandId, "Coffee", 7).Value;
        var unavailableProduct = CoinProduct.Create(brandId, "Cake", 10).Value;
        var inactiveProduct = CoinProduct.Create(brandId, "Tea", 1).Value;
        inactiveProduct.Deactivate();
        productRepository.Add(availableProduct);
        productRepository.Add(unavailableProduct);
        productRepository.Add(inactiveProduct);
        productRepository.Add(CoinProduct.Create(otherBrandId, "Other", 1).Value);

        var availableMetric = MetricBalance.Create(user.Id, brandId, Guid.NewGuid()).Value;
        availableMetric.SetMaterializedValue(5);
        var unavailableMetric = MetricBalance.Create(user.Id, brandId, Guid.NewGuid()).Value;
        unavailableMetric.SetMaterializedValue(1);
        metricBalanceRepository.SetMetricReadModel(availableMetric.MetricDefinitionId, "Visit", 5);
        metricBalanceRepository.SetMetricReadModel(unavailableMetric.MetricDefinitionId, "Dessert", 3);
        metricBalanceRepository.Add(availableMetric);
        metricBalanceRepository.Add(unavailableMetric);

        var handler = new GetUserBrandRewardsHandler(
            productRepository,
            walletRepository,
            brandRepository,
            metricBalanceRepository,
            userRepository);

        var result = await handler.Handle(
            new GetUserBrandRewardsQuery(user.Id, brandId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsMetricsEnabled);
        Assert.True(result.Value.IsCoinsEnabled);
        Assert.Equal(8, result.Value.CoinBalance);
        Assert.Equal(2, result.Value.CoinProducts.Count);

        var coffee = result.Value.CoinProducts.Single(product => product.ProductId == availableProduct.Id);
        Assert.True(coffee.IsAvailable);
        Assert.Equal(0, coffee.MissingAmount);

        var cake = result.Value.CoinProducts.Single(product => product.ProductId == unavailableProduct.Id);
        Assert.False(cake.IsAvailable);
        Assert.Equal(2, cake.MissingAmount);

        Assert.DoesNotContain(result.Value.CoinProducts, product => product.ProductId == inactiveProduct.Id);
        Assert.Equal(2, result.Value.Metrics.Count);
        Assert.Contains(result.Value.Metrics, metric => metric.CurrentBalance == 5 && metric.IsAvailable);
        Assert.Contains(result.Value.Metrics, metric => metric.CurrentBalance == 1 && metric.MissingAmount == 2);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldFail()
    {
        var handler = new GetUserBrandRewardsHandler(
            new FakeCoinProductRepository(),
            new FakeCoinWalletRepository(),
            new FakeBrandRepository(),
            new FakeMetricBalanceRepository(),
            new FakeUserRepository());

        var result = await handler.Handle(
            new GetUserBrandRewardsQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task Handle_WhenMetricsAreDisabled_ShouldNotReturnMetricSectionData()
    {
        var user = User.Create("Customer", "1234").Value;
        var brand = Brand.Create("Brand").Value;
        brand.UpdateDetails("Brand", isMetricsEnabled: false, isCoinsEnabled: true);
        var userRepository = new FakeUserRepository();
        var productRepository = new FakeCoinProductRepository();
        var walletRepository = new FakeCoinWalletRepository();
        var brandRepository = new FakeBrandRepository();
        var metricBalanceRepository = new FakeMetricBalanceRepository();
        userRepository.Add(user);
        brandRepository.AddExisting(brand);

        var metric = MetricBalance.Create(user.Id, brand.Id, Guid.NewGuid()).Value;
        metric.SetMaterializedValue(5);
        metricBalanceRepository.Add(metric);

        var handler = new GetUserBrandRewardsHandler(
            productRepository,
            walletRepository,
            brandRepository,
            metricBalanceRepository,
            userRepository);

        var result = await handler.Handle(
            new GetUserBrandRewardsQuery(user.Id, brand.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsMetricsEnabled);
        Assert.True(result.Value.IsCoinsEnabled);
        Assert.Empty(result.Value.Metrics);
    }

    [Fact]
    public async Task Handle_WhenCoinProductRedemptionIsDisabled_ShouldNotReturnCoinProducts()
    {
        var user = User.Create("Customer", "1234").Value;
        var brand = Brand.Create("Brand").Value;
        brand.UpdateDetails(
            "Brand",
            isMetricsEnabled: true,
            isCoinsEnabled: true,
            isCoinProductRedemptionEnabled: false,
            isManualCoinRedemptionEnabled: true);
        var userRepository = new FakeUserRepository();
        var productRepository = new FakeCoinProductRepository();
        var walletRepository = new FakeCoinWalletRepository();
        var brandRepository = new FakeBrandRepository();
        var metricBalanceRepository = new FakeMetricBalanceRepository();
        userRepository.Add(user);
        brandRepository.AddExisting(brand);

        var wallet = CoinWallet.Create(user.Id, brand.Id).Value;
        wallet.SetMaterializedValue(10);
        walletRepository.Add(wallet);
        productRepository.Add(CoinProduct.Create(brand.Id, "Coffee", 1).Value);

        var handler = new GetUserBrandRewardsHandler(
            productRepository,
            walletRepository,
            brandRepository,
            metricBalanceRepository,
            userRepository);

        var result = await handler.Handle(
            new GetUserBrandRewardsQuery(user.Id, brand.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsCoinsEnabled);
        Assert.False(result.Value.IsCoinProductRedemptionEnabled);
        Assert.True(result.Value.IsManualCoinRedemptionEnabled);
        Assert.Equal(10, result.Value.CoinBalance);
        Assert.Empty(result.Value.CoinProducts);
    }
}
