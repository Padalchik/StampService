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
    public Guid ActorUserId { get; private set; }

    private StampTransaction(Guid metricBalanceId, StampTransactionType type, int amount, string comment, Guid actorUserId)
    {
        MetricBalanceId = metricBalanceId;
        Type = type;
        Amount = amount;
        Comment = comment;
        ActorUserId = actorUserId;
    }

    // EF Core
    protected StampTransaction()
    {
        Comment = null!;
    }

    public static Result<StampTransaction> CreateIssue(Guid metricBalanceId, int amount, string comment, Guid actorUserId)
    {
        return Create(metricBalanceId, StampTransactionType.Issue, amount, comment, actorUserId);
    }

    public static Result<StampTransaction> CreateRedeem(Guid metricBalanceId, int amount, string comment, Guid actorUserId)
    {
        return Create(metricBalanceId, StampTransactionType.Redeem, amount, comment, actorUserId);
    }

    private static Result<StampTransaction> Create(
        Guid metricBalanceId,
        StampTransactionType type,
        int amount,
        string comment,
        Guid actorUserId)
    {
        if (metricBalanceId == Guid.Empty)
            return Result.Fail(DomainError.Validation(
                "stamp_transaction.metric_balance_id_empty",
                "MetricBalanceId cannot be empty GUID",
                nameof(metricBalanceId)));

        if (actorUserId == Guid.Empty)
            return Result.Fail(DomainError.Validation(
                "stamp_transaction.actor_user_id_empty",
                "ActorUserId cannot be empty GUID",
                nameof(actorUserId)));

        if (amount <= 0)
            return Result.Fail(DomainError.Validation(
                "stamp_transaction.amount_not_positive",
                "Amount must be greater than zero",
                nameof(amount)));

        if (string.IsNullOrWhiteSpace(comment))
            return Result.Fail(DomainError.Validation(
                "stamp_transaction.comment_required",
                "Comment cannot be empty",
                nameof(comment)));

        if (comment.Length > Constants.MAX_TRANSACTION_COMMENT_LENGTH)
            return Result.Fail(DomainError.Validation(
                "stamp_transaction.comment_too_long",
                $"Comment must not exceed {Constants.MAX_TRANSACTION_COMMENT_LENGTH} characters",
                nameof(comment)));

        return Result.Ok(new StampTransaction(metricBalanceId, type, amount, comment.Trim(), actorUserId));
    }
}
