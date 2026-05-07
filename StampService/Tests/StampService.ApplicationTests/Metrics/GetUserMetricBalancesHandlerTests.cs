using StampService.Application.Metrics.Queries.GetUserMetricBalances;
using StampService.ApplicationTests.Fakes;
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
        var handler = new GetUserMetricBalancesHandler(balanceRepository, userRepository);

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
            userRepository);

        var result = await handler.Handle(
            new GetUserMetricBalancesQuery(user.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Balances);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldFail()
    {
        var handler = new GetUserMetricBalancesHandler(
            new FakeMetricBalanceRepository(),
            new FakeUserRepository());

        var result = await handler.Handle(
            new GetUserMetricBalancesQuery(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }
}
