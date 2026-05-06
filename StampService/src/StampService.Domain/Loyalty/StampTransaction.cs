using FluentResults;
using StampService.Domain.Shared;

namespace StampService.Domain.Loyalty;

public class StampTransaction : BaseEntity
{
    public Guid MetricBalanceId { get; private set; }
    public MetricBalance MetricBalance { get; private set; } = null!;
    public StampTransactionType Type { get; private set; }
    public int Amount { get; private set; }
    public string Comment { get; private set; }

    private StampTransaction(Guid metricBalanceId, StampTransactionType type, int amount, string comment)
    {
        MetricBalanceId = metricBalanceId;
        Type = type;
        Amount = amount;
        Comment = comment;
    }

    // EF Core
    protected StampTransaction()
    {
        Comment = null!;
    }

    public static Result<StampTransaction> CreateIssue(Guid metricBalanceId, int amount, string comment)
    {
        return Create(metricBalanceId, StampTransactionType.Issue, amount, comment);
    }

    public static Result<StampTransaction> CreateRedeem(Guid metricBalanceId, int amount, string comment)
    {
        return Create(metricBalanceId, StampTransactionType.Redeem, amount, comment);
    }

    private static Result<StampTransaction> Create(
        Guid metricBalanceId,
        StampTransactionType type,
        int amount,
        string comment)
    {
        if (metricBalanceId == Guid.Empty)
            return Result.Fail("MetricBalanceId cannot be empty GUID");

        if (amount <= 0)
            return Result.Fail("Amount must be greater than zero");

        if (string.IsNullOrWhiteSpace(comment))
            return Result.Fail("Comment cannot be empty");

        if (comment.Length > Constants.MAX_TRANSACTION_COMMENT_LENGTH)
            return Result.Fail($"Comment must not exceed {Constants.MAX_TRANSACTION_COMMENT_LENGTH} characters");

        return Result.Ok(new StampTransaction(metricBalanceId, type, amount, comment.Trim()));
    }
}
