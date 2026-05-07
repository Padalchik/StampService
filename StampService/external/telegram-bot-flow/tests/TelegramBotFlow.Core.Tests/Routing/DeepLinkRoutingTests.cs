using FluentAssertions;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.Routing;

namespace TelegramBotFlow.Core.Tests.Routing;

public sealed class DeepLinkRoutingTests
{
    private static readonly UpdateDelegate NoOp = _ => Task.CompletedTask;

    [Fact]
    public void DeepLinkRoute_MatchesStartWithPayload()
    {
        var route = RouteEntry.DeepLink("start", NoOp);
        var ctx = TestHelpers.CreateMessageContext("/start ref_abc123");

        route.Matches(ctx).Should().BeTrue();
    }

    [Fact]
    public void DeepLinkRoute_DoesNotMatchStartWithoutPayload()
    {
        var route = RouteEntry.DeepLink("start", NoOp);
        var ctx = TestHelpers.CreateMessageContext("/start");

        route.Matches(ctx).Should().BeFalse();
    }

    [Fact]
    public void DeepLinkRoute_HasHighPriority()
    {
        var route = RouteEntry.DeepLink("start", NoOp);

        route.Priority.Should().Be(RoutePriority.HIGH);
    }
}
