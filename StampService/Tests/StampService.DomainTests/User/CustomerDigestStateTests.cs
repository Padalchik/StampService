using StampService.Domain.User;

namespace StampService.DomainTests.User;

public class CustomerDigestStateTests
{
    [Fact]
    public void CanSendDigest_WhenWalletWasNotOpened_ShouldReturnFalse()
    {
        var state = CustomerDigestState.Create(Guid.NewGuid()).Value;

        var result = state.CanSendDigest(
            new DateTime(2026, 5, 15, 10, 0, 0, DateTimeKind.Utc),
            TimeSpan.FromDays(7));

        Assert.False(result);
    }

    [Fact]
    public void CanSendDigest_WhenWalletAndDigestIntervalsPassed_ShouldReturnTrue()
    {
        var now = new DateTime(2026, 5, 15, 10, 0, 0, DateTimeKind.Utc);
        var state = CustomerDigestState.Create(Guid.NewGuid()).Value;
        state.MarkWalletOpened(now.AddDays(-8));
        state.MarkDigestSent(now.AddDays(-7));

        var result = state.CanSendDigest(now, TimeSpan.FromDays(7));

        Assert.True(result);
    }

    [Fact]
    public void CanSendDigest_WhenWalletWasOpenedRecently_ShouldReturnFalse()
    {
        var now = new DateTime(2026, 5, 15, 10, 0, 0, DateTimeKind.Utc);
        var state = CustomerDigestState.Create(Guid.NewGuid()).Value;
        state.MarkWalletOpened(now.AddDays(-3));
        state.MarkDigestSent(now.AddDays(-8));

        var result = state.CanSendDigest(now, TimeSpan.FromDays(7));

        Assert.False(result);
    }
}
