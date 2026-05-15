using FluentResults;

namespace StampService.Domain.User;

public class CustomerDigestState
{
    public Guid UserId { get; private set; }
    public DateTime? LastDigestSentAtUtc { get; private set; }
    public DateTime? LastWalletOpenedAtUtc { get; private set; }

    public User User { get; private set; } = null!;

    private CustomerDigestState(Guid userId)
    {
        UserId = userId;
    }

    protected CustomerDigestState()
    {
    }

    public static Result<CustomerDigestState> Create(Guid userId)
    {
        if (userId == Guid.Empty)
            return Result.Fail("UserId cannot be empty");

        return Result.Ok(new CustomerDigestState(userId));
    }

    public void MarkWalletOpened(DateTime nowUtc)
    {
        LastWalletOpenedAtUtc = nowUtc;
    }

    public void MarkDigestSent(DateTime nowUtc)
    {
        LastDigestSentAtUtc = nowUtc;
    }

    public bool CanSendDigest(DateTime nowUtc, TimeSpan interval)
    {
        if (LastWalletOpenedAtUtc is null)
            return false;

        if (nowUtc - LastWalletOpenedAtUtc.Value < interval)
            return false;

        return LastDigestSentAtUtc is null || nowUtc - LastDigestSentAtUtc.Value >= interval;
    }
}
