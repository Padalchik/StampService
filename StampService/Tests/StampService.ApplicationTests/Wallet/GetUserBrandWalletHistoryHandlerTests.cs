using StampService.Application.Wallet.Queries.GetUserBrandWalletHistory;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Coins;
using StampService.Domain.Loyalty;
using StampService.Domain.Shared;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Wallet;

public class GetUserBrandWalletHistoryHandlerTests
{
    [Fact]
    public async Task Handle_WhenBrandHasMetricAndCoinTransactions_ShouldReturnUnifiedHistoryNewestFirst()
    {
        var user = User.Create("Customer", "1234").Value;
        var brandId = Guid.NewGuid();
        var otherBrandId = Guid.NewGuid();
        var metricId = Guid.NewGuid();
        var otherMetricId = Guid.NewGuid();
        var userRepository = new FakeUserRepository();
        var metricBalanceRepository = new FakeMetricBalanceRepository();
        var stampTransactionRepository = new FakeStampTransactionRepository();
        var coinWalletRepository = new FakeCoinWalletRepository();
        var coinTransactionRepository = new FakeCoinTransactionRepository();
        userRepository.Add(user);

        var metricBalance = MetricBalance.Create(user.Id, brandId, metricId).Value;
        var otherMetricBalance = MetricBalance.Create(user.Id, otherBrandId, otherMetricId).Value;
        metricBalanceRepository.Add(metricBalance);
        metricBalanceRepository.Add(otherMetricBalance);

        var oldMetricIssue = StampTransaction.CreateIssue(metricBalance.Id, 3, "metric issue", Guid.NewGuid()).Value;
        SetCreatedAt(oldMetricIssue, new DateTime(2026, 5, 8, 10, 0, 0, DateTimeKind.Utc));
        var middleMetricRedeem = StampTransaction.CreateRedeem(metricBalance.Id, 1, "metric redeem", Guid.NewGuid()).Value;
        SetCreatedAt(middleMetricRedeem, new DateTime(2026, 5, 8, 11, 0, 0, DateTimeKind.Utc));
        var otherBrandMetricIssue = StampTransaction.CreateIssue(otherMetricBalance.Id, 9, "other brand", Guid.NewGuid()).Value;
        SetCreatedAt(otherBrandMetricIssue, new DateTime(2026, 5, 8, 12, 0, 0, DateTimeKind.Utc));
        stampTransactionRepository.Add(oldMetricIssue);
        stampTransactionRepository.Add(middleMetricRedeem);
        stampTransactionRepository.Add(otherBrandMetricIssue);

        var wallet = CoinWallet.Create(user.Id, brandId).Value;
        var otherWallet = CoinWallet.Create(user.Id, otherBrandId).Value;
        coinWalletRepository.Add(wallet);
        coinWalletRepository.Add(otherWallet);

        var newestCoinIssue = CoinTransaction.CreateIssue(wallet.Id, 10, "coin issue", Guid.NewGuid()).Value;
        SetCreatedAt(newestCoinIssue, new DateTime(2026, 5, 8, 12, 30, 0, DateTimeKind.Utc));
        var otherBrandCoinIssue = CoinTransaction.CreateIssue(otherWallet.Id, 99, "other coin", Guid.NewGuid()).Value;
        SetCreatedAt(otherBrandCoinIssue, new DateTime(2026, 5, 8, 13, 0, 0, DateTimeKind.Utc));
        coinTransactionRepository.Add(newestCoinIssue);
        coinTransactionRepository.Add(otherBrandCoinIssue);

        var handler = new GetUserBrandWalletHistoryHandler(
            coinTransactionRepository,
            coinWalletRepository,
            metricBalanceRepository,
            stampTransactionRepository,
            userRepository);

        var result = await handler.Handle(
            new GetUserBrandWalletHistoryQuery(user.Id, brandId, Skip: 0, Take: 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(brandId, result.Value.BrandId);
        Assert.Equal(3, result.Value.Items.Count);

        var items = result.Value.Items.ToArray();
        Assert.Equal("Coin", items[0].SourceType);
        Assert.Equal("монетки", items[0].SourceName);
        Assert.Equal(CoinTransactionType.Issue.ToString(), items[0].TransactionType);
        Assert.Equal(10, items[0].Amount);

        Assert.Equal("Metric", items[1].SourceType);
        Assert.Equal(StampTransactionType.Redeem.ToString(), items[1].TransactionType);
        Assert.Equal(1, items[1].Amount);

        Assert.Equal("Metric", items[2].SourceType);
        Assert.Equal(StampTransactionType.Issue.ToString(), items[2].TransactionType);
        Assert.Equal(3, items[2].Amount);

        Assert.DoesNotContain(result.Value.Items, item => item.Amount is 9 or 99);
    }

    [Fact]
    public async Task Handle_WhenSkipAndTakeAreProvided_ShouldPageUnifiedHistoryAfterSorting()
    {
        var user = User.Create("Customer", "1234").Value;
        var brandId = Guid.NewGuid();
        var userRepository = new FakeUserRepository();
        var metricBalanceRepository = new FakeMetricBalanceRepository();
        var stampTransactionRepository = new FakeStampTransactionRepository();
        var coinWalletRepository = new FakeCoinWalletRepository();
        var coinTransactionRepository = new FakeCoinTransactionRepository();
        userRepository.Add(user);

        var metricBalance = MetricBalance.Create(user.Id, brandId, Guid.NewGuid()).Value;
        metricBalanceRepository.Add(metricBalance);
        var first = StampTransaction.CreateIssue(metricBalance.Id, 1, "first", Guid.NewGuid()).Value;
        SetCreatedAt(first, new DateTime(2026, 5, 8, 10, 0, 0, DateTimeKind.Utc));
        var second = StampTransaction.CreateIssue(metricBalance.Id, 2, "second", Guid.NewGuid()).Value;
        SetCreatedAt(second, new DateTime(2026, 5, 8, 11, 0, 0, DateTimeKind.Utc));
        var third = StampTransaction.CreateIssue(metricBalance.Id, 3, "third", Guid.NewGuid()).Value;
        SetCreatedAt(third, new DateTime(2026, 5, 8, 12, 0, 0, DateTimeKind.Utc));
        stampTransactionRepository.Add(first);
        stampTransactionRepository.Add(second);
        stampTransactionRepository.Add(third);

        var handler = new GetUserBrandWalletHistoryHandler(
            coinTransactionRepository,
            coinWalletRepository,
            metricBalanceRepository,
            stampTransactionRepository,
            userRepository);

        var result = await handler.Handle(
            new GetUserBrandWalletHistoryQuery(user.Id, brandId, Skip: 1, Take: 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(2, item.Amount);
        Assert.Equal("second", item.Comment);
    }

    [Fact]
    public async Task Handle_WhenBrandHasNoWalletOrMetricBalances_ShouldReturnEmptyHistory()
    {
        var user = User.Create("Customer", "1234").Value;
        var userRepository = new FakeUserRepository();
        userRepository.Add(user);
        var handler = new GetUserBrandWalletHistoryHandler(
            new FakeCoinTransactionRepository(),
            new FakeCoinWalletRepository(),
            new FakeMetricBalanceRepository(),
            new FakeStampTransactionRepository(),
            userRepository);

        var result = await handler.Handle(
            new GetUserBrandWalletHistoryQuery(user.Id, Guid.NewGuid(), Skip: 0, Take: 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(0, 0)]
    [InlineData(0, 101)]
    public async Task Handle_WhenPagingIsInvalid_ShouldFail(int skip, int take)
    {
        var handler = new GetUserBrandWalletHistoryHandler(
            new FakeCoinTransactionRepository(),
            new FakeCoinWalletRepository(),
            new FakeMetricBalanceRepository(),
            new FakeStampTransactionRepository(),
            new FakeUserRepository());

        var result = await handler.Handle(
            new GetUserBrandWalletHistoryQuery(Guid.NewGuid(), Guid.NewGuid(), skip, take),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldFail()
    {
        var handler = new GetUserBrandWalletHistoryHandler(
            new FakeCoinTransactionRepository(),
            new FakeCoinWalletRepository(),
            new FakeMetricBalanceRepository(),
            new FakeStampTransactionRepository(),
            new FakeUserRepository());

        var result = await handler.Handle(
            new GetUserBrandWalletHistoryQuery(Guid.NewGuid(), Guid.NewGuid(), Skip: 0, Take: 10),
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
