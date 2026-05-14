using StampService.Application.Wallet.Queries.GetUserWalletOverview;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Coins;
using StampService.Domain.Loyalty;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Wallet;

public class GetUserWalletOverviewHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnOnlyAvailableProductsAndMetrics()
    {
        var user = User.Create("Customer", "1234").Value;
        var brandId = Guid.NewGuid();
        var userRepository = new FakeUserRepository();
        var productRepository = new FakeCoinProductRepository();
        var walletRepository = new FakeCoinWalletRepository();
        var metricBalanceRepository = new FakeMetricBalanceRepository();
        userRepository.Add(user);

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

        var availableMetric = MetricBalance.Create(user.Id, brandId, Guid.NewGuid()).Value;
        availableMetric.SetMaterializedValue(5);
        var unavailableMetric = MetricBalance.Create(user.Id, brandId, Guid.NewGuid()).Value;
        unavailableMetric.SetMaterializedValue(1);
        metricBalanceRepository.SetMetricReadModel(availableMetric.MetricDefinitionId, "Visit", 5);
        metricBalanceRepository.SetMetricReadModel(unavailableMetric.MetricDefinitionId, "Dessert", 3);
        metricBalanceRepository.Add(availableMetric);
        metricBalanceRepository.Add(unavailableMetric);

        var handler = new GetUserWalletOverviewHandler(
            productRepository,
            walletRepository,
            metricBalanceRepository,
            userRepository);

        var result = await handler.Handle(
            new GetUserWalletOverviewQuery(user.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var brand = Assert.Single(result.Value.Brands);
        Assert.Equal(brandId, brand.BrandId);
        Assert.Equal(8, brand.CoinBalance);

        var product = Assert.Single(brand.AvailableCoinProducts);
        Assert.Equal(availableProduct.Id, product.ProductId);
        Assert.DoesNotContain(brand.AvailableCoinProducts, item => item.ProductId == unavailableProduct.Id);
        Assert.DoesNotContain(brand.AvailableCoinProducts, item => item.ProductId == inactiveProduct.Id);

        var metric = Assert.Single(brand.AvailableMetrics);
        Assert.Equal(availableMetric.MetricDefinitionId, metric.MetricDefinitionId);
        Assert.DoesNotContain(brand.AvailableMetrics, item => item.MetricDefinitionId == unavailableMetric.MetricDefinitionId);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldFail()
    {
        var handler = new GetUserWalletOverviewHandler(
            new FakeCoinProductRepository(),
            new FakeCoinWalletRepository(),
            new FakeMetricBalanceRepository(),
            new FakeUserRepository());

        var result = await handler.Handle(
            new GetUserWalletOverviewQuery(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }
}
