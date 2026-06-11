using StampService.Application.Wallet.Queries.GetUserWalletBrandDetails;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Brand;
using StampService.Domain.Coins;
using StampService.Domain.Loyalty;
using StampService.Domain.Shared;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Wallet;

public class GetUserWalletBrandDetailsHandlerTests
{
    [Fact]
    public async Task Handle_WhenBrandHasRewardsAndHistory_ShouldReturnPresentationDetails()
    {
        var user = User.Create("Customer").Value;
        var brand = Brand.Create("Brand").Value;
        var userRepository = new FakeUserRepository();
        var brandRepository = new FakeBrandRepository();
        var productRepository = new FakeCoinProductRepository();
        var walletRepository = new FakeCoinWalletRepository();
        var coinTransactionRepository = new FakeCoinTransactionRepository();
        var metricRepository = new FakeLoyaltyMetricRepository();
        var metricBalanceRepository = new FakeMetricBalanceRepository();
        var stampTransactionRepository = new FakeStampTransactionRepository();
        userRepository.Add(user);
        brandRepository.AddExisting(brand);

        var wallet = CoinWallet.Create(user.Id, brand.Id).Value;
        wallet.SetMaterializedValue(8);
        walletRepository.Add(wallet);

        var availableProduct = CoinProduct.Create(brand.Id, "Coffee", 7).Value;
        var unavailableProduct = CoinProduct.Create(brand.Id, "Cake", 10).Value;
        productRepository.Add(availableProduct);
        productRepository.Add(unavailableProduct);

        var metric = LoyaltyMetricDefinition.Create(brand.Id, "Visit", 5).Value;
        var zeroMetric = LoyaltyMetricDefinition.Create(brand.Id, "Massage", 4).Value;
        metricRepository.AddExisting(metric);
        metricRepository.AddExisting(zeroMetric);

        var metricBalance = MetricBalance.Create(user.Id, brand.Id, metric.Id).Value;
        metricBalance.SetMaterializedValue(2);
        metricBalanceRepository.SetMetricReadModel(metricBalance.MetricDefinitionId, "Visit", 5);
        metricBalanceRepository.Add(metricBalance);

        var coinIssue = CoinTransaction.CreateIssue(wallet.Id, 8, "Issue coins", Guid.NewGuid()).Value;
        SetCreatedAt(coinIssue, new DateTime(2026, 5, 8, 12, 0, 0, DateTimeKind.Utc));
        coinTransactionRepository.Add(coinIssue);

        var metricRedeem = StampTransaction.CreateRedeem(metricBalance.Id, 1, "manual reward", Guid.NewGuid()).Value;
        SetCreatedAt(metricRedeem, new DateTime(2026, 5, 8, 11, 0, 0, DateTimeKind.Utc));
        stampTransactionRepository.Add(metricRedeem);

        var handler = new GetUserWalletBrandDetailsHandler(
            productRepository,
            coinTransactionRepository,
            walletRepository,
            brandRepository,
            metricRepository,
            metricBalanceRepository,
            stampTransactionRepository,
            userRepository);

        var result = await handler.Handle(
            new GetUserWalletBrandDetailsQuery(user.Id, brand.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(brand.Id, result.Value.BrandId);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.BrandName));
        Assert.True(result.Value.IsMetricsEnabled);
        Assert.True(result.Value.IsCoinsEnabled);
        Assert.True(result.Value.IsCoinProductRedemptionEnabled);
        Assert.Equal(8, result.Value.CoinBalance);

        var coinSection = Assert.Single(result.Value.RewardSections, section => section.Kind == "CoinProducts");
        Assert.Equal("Монетки: 8", coinSection.BalanceText);
        Assert.Contains(coinSection.Items, item => item.Name == "Coffee" && item.StatusText == "доступно");
        Assert.Contains(coinSection.Items, item => item.Name == "Cake" && item.StatusText == "не хватает 2");

        var metricSection = Assert.Single(result.Value.RewardSections, section => section.Kind == "Metrics");
        Assert.Equal(2, metricSection.Items.Count);
        var metricItem = Assert.Single(metricSection.Items, item => item.Name == "Visit");
        Assert.Equal("2/5", metricItem.ProgressText);
        Assert.Equal("не хватает 3", metricItem.StatusText);
        Assert.Contains(metricSection.Items, item =>
            item.Name == "Massage"
            && item.ProgressText == "0/4"
            && item.StatusText == "не хватает 4"
            && item.IsAvailable == false);

        var coinHistory = Assert.Single(result.Value.History.Groups, group => group.Kind == "Coin");
        var coinHistoryItem = Assert.Single(coinHistory.Items);
        Assert.Equal("+8 монетки", coinHistoryItem.AmountText);
        Assert.False(coinHistoryItem.HasVisibleComment);

        var metricHistory = Assert.Single(result.Value.History.Groups, group => group.Kind == "Metric");
        var metricHistoryItem = Assert.Single(metricHistory.Items);
        Assert.Equal("-1 Visit", metricHistoryItem.AmountText);
        Assert.True(metricHistoryItem.HasVisibleComment);
        Assert.Equal("manual reward", metricHistoryItem.Comment);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldFail()
    {
        var handler = new GetUserWalletBrandDetailsHandler(
            new FakeCoinProductRepository(),
            new FakeCoinTransactionRepository(),
            new FakeCoinWalletRepository(),
            new FakeBrandRepository(),
            new FakeLoyaltyMetricRepository(),
            new FakeMetricBalanceRepository(),
            new FakeStampTransactionRepository(),
            new FakeUserRepository());

        var result = await handler.Handle(
            new GetUserWalletBrandDetailsQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task Handle_WhenRewardFeaturesAreDisabled_ShouldNotReturnDisabledRewardSections()
    {
        var user = User.Create("Customer").Value;
        var brand = Brand.Create("Brand").Value;
        var updateResult = brand.UpdateDetails(
            "Brand",
            isMetricsEnabled: false,
            isCoinsEnabled: true,
            isCoinProductRedemptionEnabled: false,
            isManualCoinRedemptionEnabled: true);
        Assert.True(updateResult.IsSuccess);

        var userRepository = new FakeUserRepository();
        var brandRepository = new FakeBrandRepository();
        var productRepository = new FakeCoinProductRepository();
        var walletRepository = new FakeCoinWalletRepository();
        userRepository.Add(user);
        brandRepository.AddExisting(brand);

        var wallet = CoinWallet.Create(user.Id, brand.Id).Value;
        wallet.SetMaterializedValue(10);
        walletRepository.Add(wallet);
        productRepository.Add(CoinProduct.Create(brand.Id, "Coffee", 7).Value);

        var handler = new GetUserWalletBrandDetailsHandler(
            productRepository,
            new FakeCoinTransactionRepository(),
            walletRepository,
            brandRepository,
            new FakeLoyaltyMetricRepository(),
            new FakeMetricBalanceRepository(),
            new FakeStampTransactionRepository(),
            userRepository);

        var result = await handler.Handle(
            new GetUserWalletBrandDetailsQuery(user.Id, brand.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsMetricsEnabled);
        Assert.True(result.Value.IsCoinsEnabled);
        Assert.False(result.Value.IsCoinProductRedemptionEnabled);
        Assert.Equal(10, result.Value.CoinBalance);
        Assert.DoesNotContain(result.Value.RewardSections, section => section.Kind == "Metrics");
        Assert.DoesNotContain(result.Value.RewardSections, section => section.Kind == "CoinProducts");
        Assert.Contains(result.Value.History.Groups, group => group.Kind == "Coin");
    }

    private static void SetCreatedAt(BaseEntity entity, DateTime createdAt)
    {
        typeof(BaseEntity)
            .GetProperty(nameof(BaseEntity.CreatedAt))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(entity, new object[] { createdAt });
    }
}
