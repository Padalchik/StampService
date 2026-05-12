using StampService.Application.Metrics;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Loyalty;

namespace StampService.ApplicationTests.Metrics;

public class MetricLedgerServiceTests
{
    [Fact]
    public async Task IssueAsync_WhenBalanceDoesNotExist_ShouldCreateBalanceAndIssueTransaction()
    {
        var balanceRepository = new FakeMetricBalanceRepository();
        var transactionRepository = new FakeStampTransactionRepository();
        var service = new MetricLedgerService(balanceRepository, transactionRepository);

        var result = await service.IssueAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            5,
            "Issue stamps",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(balanceRepository.Balances);
        Assert.Single(transactionRepository.Transactions);
        Assert.Equal(5, result.Value.Balance.Value);
        Assert.Equal(StampTransactionType.Issue, result.Value.Transaction.Type);
        Assert.Equal(5, result.Value.Transaction.Amount);
    }

    [Fact]
    public async Task IssueAsync_WhenBalanceExists_ShouldSynchronizeFromLedgerBeforeAdding()
    {
        var userId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var metricDefinitionId = Guid.NewGuid();
        var balanceRepository = new FakeMetricBalanceRepository();
        var transactionRepository = new FakeStampTransactionRepository();
        var balance = MetricBalance.Create(userId, brandId, metricDefinitionId).Value;
        balance.SetMaterializedValue(100);
        balanceRepository.Add(balance);
        transactionRepository.Add(StampTransaction.CreateIssue(balance.Id, 3, "Existing issue", Guid.NewGuid()).Value);
        var service = new MetricLedgerService(balanceRepository, transactionRepository);

        var result = await service.IssueAsync(
            userId,
            Guid.NewGuid(),
            brandId,
            metricDefinitionId,
            2,
            "New issue",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Balance.Value);
        Assert.Equal(2, transactionRepository.Transactions.Count);
    }

    [Fact]
    public async Task RedeemAsync_WhenBalanceExists_ShouldSynchronizeSubtractAndCreateRedeemTransaction()
    {
        var userId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var metricDefinitionId = Guid.NewGuid();
        var balanceRepository = new FakeMetricBalanceRepository();
        var transactionRepository = new FakeStampTransactionRepository();
        var balance = MetricBalance.Create(userId, brandId, metricDefinitionId).Value;
        balanceRepository.Add(balance);
        transactionRepository.Add(StampTransaction.CreateIssue(balance.Id, 5, "Existing issue", Guid.NewGuid()).Value);
        var service = new MetricLedgerService(balanceRepository, transactionRepository);

        var result = await service.RedeemAsync(
            userId,
            Guid.NewGuid(),
            brandId,
            metricDefinitionId,
            3,
            "Redeem stamps",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Balance.Value);
        Assert.Equal(2, transactionRepository.Transactions.Count);
        Assert.Equal(StampTransactionType.Redeem, result.Value.Transaction.Type);
    }

    [Fact]
    public async Task RedeemAsync_WhenBalanceDoesNotExist_ShouldFail()
    {
        var service = new MetricLedgerService(
            new FakeMetricBalanceRepository(),
            new FakeStampTransactionRepository());

        var result = await service.RedeemAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "Redeem stamps",
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task RedeemAsync_WhenInsufficientBalance_ShouldFailAndNotCreateTransaction()
    {
        var userId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var metricDefinitionId = Guid.NewGuid();
        var balanceRepository = new FakeMetricBalanceRepository();
        var transactionRepository = new FakeStampTransactionRepository();
        var balance = MetricBalance.Create(userId, brandId, metricDefinitionId).Value;
        balanceRepository.Add(balance);
        transactionRepository.Add(StampTransaction.CreateIssue(balance.Id, 2, "Existing issue", Guid.NewGuid()).Value);
        var service = new MetricLedgerService(balanceRepository, transactionRepository);

        var result = await service.RedeemAsync(
            userId,
            Guid.NewGuid(),
            brandId,
            metricDefinitionId,
            3,
            "Redeem stamps",
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(transactionRepository.Transactions);
        Assert.Equal(2, balance.Value);
    }

    [Fact]
    public async Task RecalculateMetricBalanceAsync_ShouldSetMaterializedValueFromTransactions()
    {
        var userId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var metricDefinitionId = Guid.NewGuid();
        var balanceRepository = new FakeMetricBalanceRepository();
        var transactionRepository = new FakeStampTransactionRepository();
        var balance = MetricBalance.Create(userId, brandId, metricDefinitionId).Value;
        balance.SetMaterializedValue(100);
        balanceRepository.Add(balance);
        transactionRepository.Add(StampTransaction.CreateIssue(balance.Id, 10, "Issue", Guid.NewGuid()).Value);
        transactionRepository.Add(StampTransaction.CreateRedeem(balance.Id, 4, "Redeem", Guid.NewGuid()).Value);
        var service = new MetricLedgerService(balanceRepository, transactionRepository);

        var result = await service.RecalculateMetricBalanceAsync(balance.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(6, result.Value);
        Assert.Equal(6, balance.Value);
    }
}
