using StampService.Application.Metrics.Queries.GetUserMetricTransactions;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Loyalty;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Metrics;

public class GetUserMetricTransactionsHandlerTests
{
    [Fact]
    public async Task Handle_WhenBalanceHasTransactions_ShouldReturnHistory()
    {
        var user = User.Create("user").Value;
        var brandId = Guid.NewGuid();
        var metric = LoyaltyMetricDefinition.Create(brandId, "stamp", "Stamps").Value;
        var balance = MetricBalance.Create(user.Id, brandId, metric.Id).Value;
        var userRepository = new FakeUserRepository();
        var metricRepository = new FakeLoyaltyMetricRepository();
        var balanceRepository = new FakeMetricBalanceRepository();
        var transactionRepository = new FakeStampTransactionRepository();
        userRepository.Add(user);
        metricRepository.AddExisting(metric);
        balanceRepository.Add(balance);
        transactionRepository.Add(StampTransaction.CreateIssue(balance.Id, 5, "Issue").Value);

        var handler = new GetUserMetricTransactionsHandler(
            metricRepository,
            balanceRepository,
            transactionRepository,
            userRepository);

        var result = await handler.Handle(
            new GetUserMetricTransactionsQuery(metric.Id, user.Id, 0, 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("Issue", result.Value.Items.Single().TransactionType);
        Assert.Equal(5, result.Value.Items.Single().Amount);
    }

    [Fact]
    public async Task Handle_WhenBalanceDoesNotExist_ShouldReturnEmptyHistory()
    {
        var user = User.Create("user").Value;
        var metric = LoyaltyMetricDefinition.Create(Guid.NewGuid(), "stamp", "Stamps").Value;
        var userRepository = new FakeUserRepository();
        var metricRepository = new FakeLoyaltyMetricRepository();
        userRepository.Add(user);
        metricRepository.AddExisting(metric);
        var handler = new GetUserMetricTransactionsHandler(
            metricRepository,
            new FakeMetricBalanceRepository(),
            new FakeStampTransactionRepository(),
            userRepository);

        var result = await handler.Handle(
            new GetUserMetricTransactionsQuery(metric.Id, user.Id, 0, 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task Handle_WhenTakeIsTooLarge_ShouldFail()
    {
        var handler = new GetUserMetricTransactionsHandler(
            new FakeLoyaltyMetricRepository(),
            new FakeMetricBalanceRepository(),
            new FakeStampTransactionRepository(),
            new FakeUserRepository());

        var result = await handler.Handle(
            new GetUserMetricTransactionsQuery(Guid.NewGuid(), Guid.NewGuid(), 0, 101),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }
}
