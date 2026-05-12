using StampService.Application.Metrics.Queries.GetUserMetricBalances;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Coins;
using StampService.Domain.Loyalty;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Metrics;

public class GetUserMetricBalancesHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserHasBalances_ShouldReturnThem()
    {
        var user = User.Create("user").Value;
        var balanceRepository = new FakeMetricBalanceRepository();
        var userRepository = new FakeUserRepository();
        userRepository.Add(user);
        var balance = MetricBalance.Create(
            user.Id,
            Guid.NewGuid(),
            Guid.NewGuid()).Value;
        balance.SetMaterializedValue(7);
        balanceRepository.Add(balance);
        var handler = new GetUserMetricBalancesHandler(
            balanceRepository,
            new FakeCoinWalletRepository(),
            userRepository);

        var result = await handler.Handle(
            new GetUserMetricBalancesQuery(user.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Balances);
        Assert.Equal(7, result.Value.Balances.Single().Value);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoBalances_ShouldReturnEmptyList()
    {
        var user = User.Create("user").Value;
        var userRepository = new FakeUserRepository();
        userRepository.Add(user);
        var handler = new GetUserMetricBalancesHandler(
            new FakeMetricBalanceRepository(),
            new FakeCoinWalletRepository(),
            userRepository);

        var result = await handler.Handle(
            new GetUserMetricBalancesQuery(user.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Balances);
        Assert.Empty(result.Value.CoinWallets);
    }

    [Fact]
    public async Task Handle_WhenUserHasCoinWallets_ShouldReturnThem()
    {
        var user = User.Create("user").Value;
        var brandId = Guid.NewGuid();
        var userRepository = new FakeUserRepository();
        var coinWalletRepository = new FakeCoinWalletRepository();
        userRepository.Add(user);
        var wallet = CoinWallet.Create(user.Id, brandId).Value;
        wallet.Add(11);
        coinWalletRepository.Add(wallet);
        var handler = new GetUserMetricBalancesHandler(
            new FakeMetricBalanceRepository(),
            coinWalletRepository,
            userRepository);

        var result = await handler.Handle(
            new GetUserMetricBalancesQuery(user.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Balances);
        var coinWallet = Assert.Single(result.Value.CoinWallets);
        Assert.Equal(brandId, coinWallet.BrandId);
        Assert.Equal(11, coinWallet.Value);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldFail()
    {
        var handler = new GetUserMetricBalancesHandler(
            new FakeMetricBalanceRepository(),
            new FakeCoinWalletRepository(),
            new FakeUserRepository());

        var result = await handler.Handle(
            new GetUserMetricBalancesQuery(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task Handle_WhenUserIdIsEmpty_ShouldFail()
    {
        var handler = new GetUserMetricBalancesHandler(
            new FakeMetricBalanceRepository(),
            new FakeCoinWalletRepository(),
            new FakeUserRepository());

        var result = await handler.Handle(
            new GetUserMetricBalancesQuery(Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }
}
