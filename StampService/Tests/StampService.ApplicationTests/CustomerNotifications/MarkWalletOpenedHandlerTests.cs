using StampService.Application.CustomerNotifications.Commands.MarkWalletOpened;
using StampService.ApplicationTests.Fakes;

namespace StampService.ApplicationTests.CustomerNotifications;

public class MarkWalletOpenedHandlerTests
{
    [Fact]
    public async Task Handle_WhenStateDoesNotExist_ShouldCreateStateAndMarkWalletOpened()
    {
        var now = new DateTimeOffset(2026, 5, 15, 10, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();
        var repository = new FakeCustomerDigestStateRepository();
        var handler = new MarkWalletOpenedHandler(repository, new FixedTimeProvider(now));

        var result = await handler.Handle(
            new MarkWalletOpenedCommand(userId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var state = Assert.Single(repository.States);
        Assert.Equal(userId, state.UserId);
        Assert.Equal(now.UtcDateTime, state.LastWalletOpenedAtUtc);
        Assert.Equal(now.UtcDateTime, result.Value.LastWalletOpenedAtUtc);
        Assert.Equal(1, repository.SaveCount);
    }
}
