using FluentResults;
using StampService.Domain.Shared;

namespace StampService.Domain.Coins;

public class CoinTransaction : BaseEntity
{
    public Guid CoinWalletId { get; private set; }
    public CoinWallet CoinWallet { get; private set; } = null!;
    public CoinTransactionType Type { get; private set; }
    public int Amount { get; private set; }
    public string Comment { get; private set; }
    public Guid ActorUserId { get; private set; }

    private CoinTransaction(Guid coinWalletId, CoinTransactionType type, int amount, string comment, Guid actorUserId)
    {
        CoinWalletId = coinWalletId;
        Type = type;
        Amount = amount;
        Comment = comment;
        ActorUserId = actorUserId;
    }

    // EF Core
    protected CoinTransaction()
    {
        Comment = null!;
    }

    public static Result<CoinTransaction> CreateIssue(Guid coinWalletId, int amount, string comment, Guid actorUserId)
    {
        return Create(coinWalletId, CoinTransactionType.Issue, amount, comment, actorUserId);
    }

    public static Result<CoinTransaction> CreateRedeem(Guid coinWalletId, int amount, string comment, Guid actorUserId)
    {
        return Create(coinWalletId, CoinTransactionType.Redeem, amount, comment, actorUserId);
    }

    private static Result<CoinTransaction> Create(
        Guid coinWalletId,
        CoinTransactionType type,
        int amount,
        string comment,
        Guid actorUserId)
    {
        if (coinWalletId == Guid.Empty)
            return Result.Fail(DomainError.Validation(
                "coin_transaction.coin_wallet_id_empty",
                "CoinWalletId cannot be empty GUID",
                nameof(coinWalletId)));

        if (actorUserId == Guid.Empty)
            return Result.Fail(DomainError.Validation(
                "coin_transaction.actor_user_id_empty",
                "ActorUserId cannot be empty GUID",
                nameof(actorUserId)));

        if (amount <= 0)
            return Result.Fail(DomainError.Validation(
                "coin_transaction.amount_not_positive",
                "Amount must be greater than zero",
                nameof(amount)));

        if (string.IsNullOrWhiteSpace(comment))
            return Result.Fail(DomainError.Validation(
                "coin_transaction.comment_required",
                "Comment cannot be empty",
                nameof(comment)));

        if (comment.Length > Constants.MAX_COIN_TRANSACTION_COMMENT_LENGTH)
            return Result.Fail(DomainError.Validation(
                "coin_transaction.comment_too_long",
                $"Comment must not exceed {Constants.MAX_COIN_TRANSACTION_COMMENT_LENGTH} characters",
                nameof(comment)));

        return Result.Ok(new CoinTransaction(coinWalletId, type, amount, comment.Trim(), actorUserId));
    }
}
