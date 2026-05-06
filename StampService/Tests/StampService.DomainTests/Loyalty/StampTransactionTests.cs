using StampService.Domain.Loyalty;

namespace StampService.DomainTests.Loyalty;

public class StampTransactionTests
{
    [Fact]
    public void CreateIssue_ValidData_ShouldCreateIssueTransaction()
    {
        var metricBalanceId = Guid.NewGuid();

        var result = StampTransaction.CreateIssue(metricBalanceId, 3, " Issue stamps ");

        Assert.True(result.IsSuccess);
        Assert.Equal(metricBalanceId, result.Value.MetricBalanceId);
        Assert.Equal(StampTransactionType.Issue, result.Value.Type);
        Assert.Equal(3, result.Value.Amount);
        Assert.Equal("Issue stamps", result.Value.Comment);
    }

    [Fact]
    public void CreateRedeem_ValidData_ShouldCreateRedeemTransaction()
    {
        var metricBalanceId = Guid.NewGuid();

        var result = StampTransaction.CreateRedeem(metricBalanceId, 2, "Redeem stamps");

        Assert.True(result.IsSuccess);
        Assert.Equal(metricBalanceId, result.Value.MetricBalanceId);
        Assert.Equal(StampTransactionType.Redeem, result.Value.Type);
        Assert.Equal(2, result.Value.Amount);
    }

    [Fact]
    public void CreateIssue_EmptyBalanceId_ShouldFail()
    {
        var result = StampTransaction.CreateIssue(Guid.Empty, 1, "Issue stamps");

        Assert.True(result.IsFailed);
    }

    [Fact]
    public void CreateIssue_NonPositiveAmount_ShouldFail()
    {
        var result = StampTransaction.CreateIssue(Guid.NewGuid(), 0, "Issue stamps");

        Assert.True(result.IsFailed);
    }

    [Fact]
    public void CreateIssue_EmptyComment_ShouldFail()
    {
        var result = StampTransaction.CreateIssue(Guid.NewGuid(), 1, " ");

        Assert.True(result.IsFailed);
    }
}
