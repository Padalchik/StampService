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

        var metricBalance = MetricBalance.Create(user.Id, brand.Id, Guid.NewGuid()).Value;
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
            metricBalanceRepository,
            stampTransactionRepository,
            userRepository);

        var result = await handler.Handle(
            new GetUserWalletBrandDetailsQuery(user.Id, brand.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(brand.Id, result.Value.BrandId);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.BrandName));

        var coinSection = Assert.Single(result.Value.RewardSections, section => section.Kind == "CoinProducts");
        Assert.Equal("Монетки: 8", coinSection.BalanceText);
        Assert.Contains(coinSection.Items, item => item.Name == "Coffee" && item.StatusText == "доступно");
        Assert.Contains(coinSection.Items, item => item.Name == "Cake" && item.StatusText == "не хватает 2");

        var metricSection = Assert.Single(result.Value.RewardSections, section => section.Kind == "Metrics");
        var metricItem = Assert.Single(metricSection.Items);
        Assert.Equal("2/5", metricItem.ProgressText);
        Assert.Equal("не хватает 3", metricItem.StatusText);

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
            new FakeMetricBalanceRepository(),
            new FakeStampTransactionRepository(),
            new FakeUserRepository());

        var result = await handler.Handle(
            new GetUserWalletBrandDetailsQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    private static void SetCreatedAt(BaseEntity entity, DateTime createdAt)
    {
        typeof(BaseEntity)
            .GetProperty(nameof(BaseEntity.CreatedAt))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(entity, new object[] { createdAt });
    }
}
