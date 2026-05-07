using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Routing;

namespace TelegramBotFlow.Core.Tests.Routing;

public sealed class UpdateRouterTests
{
    private readonly UpdateRouter _router = new(NullLogger<UpdateRouter>.Instance);

    [Fact]
    public async Task BuildTerminal_FirstMatchingRouteWins()
    {
        var handlerCalls = new List<string>();

        _router.AddRoute(RouteEntry.Command("/start", ctx =>
        {
            handlerCalls.Add("first");
            return Task.CompletedTask;
        }));

        _router.AddRoute(RouteEntry.Command("/start", ctx =>
        {
            handlerCalls.Add("second");
            return Task.CompletedTask;
        }));

        Core.Pipeline.UpdateDelegate terminal = _router.BuildTerminal();
        UpdateContext ctx = TestHelpers.CreateMessageContext("/start");

        await terminal(ctx);

        handlerCalls.Should().Equal("first");
    }

    [Fact]
    public async Task BuildTerminal_NoMatchDoesNotThrow()
    {
        _router.AddRoute(RouteEntry.Command("/start", _ => Task.CompletedTask));

        Core.Pipeline.UpdateDelegate terminal = _router.BuildTerminal();
        UpdateContext ctx = TestHelpers.CreateMessageContext("/unknown");

        Func<Task> act = () => terminal(ctx);

        await act.Should().NotThrowAsync();
    }
}