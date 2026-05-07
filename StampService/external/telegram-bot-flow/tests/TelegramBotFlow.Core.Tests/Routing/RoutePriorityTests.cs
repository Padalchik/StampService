using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.Routing;

namespace TelegramBotFlow.Core.Tests.Routing;

public sealed class RoutePriorityTests
{
    private readonly UpdateRouter _router = new(NullLogger<UpdateRouter>.Instance);

    [Fact]
    public async Task HighPriority_MatchesBeforeNormal()
    {
        var calls = new List<string>();

        _router.AddRoute(RouteEntry.Message(
            ctx => ctx.MessageText == "test",
            ctx =>
            {
                calls.Add("normal");
                return Task.CompletedTask;
            }));

        _router.AddRoute(RouteEntry.Message(
            ctx => ctx.MessageText == "test",
            ctx =>
            {
                calls.Add("high");
                return Task.CompletedTask;
            },
            RoutePriority.HIGH));

        UpdateDelegate terminal = _router.BuildTerminal();
        UpdateContext ctx = TestHelpers.CreateMessageContext("test");

        await terminal(ctx);

        calls.Should().Equal("high");
    }

    [Fact]
    public async Task Fallback_InvokedWhenNoRouteMatches()
    {
        var calls = new List<string>();

        _router.AddRoute(RouteEntry.Command("/start", ctx =>
        {
            calls.Add("start");
            return Task.CompletedTask;
        }));
        _router.SetFallback(ctx =>
        {
            calls.Add("fallback");
            return Task.CompletedTask;
        });

        UpdateDelegate terminal = _router.BuildTerminal();
        UpdateContext ctx = TestHelpers.CreateMessageContext("random text");

        await terminal(ctx);

        calls.Should().Equal("fallback");
    }

    [Fact]
    public async Task Fallback_NotInvokedWhenRouteMatches()
    {
        var calls = new List<string>();

        _router.AddRoute(RouteEntry.Command("/start", ctx =>
        {
            calls.Add("start");
            return Task.CompletedTask;
        }));
        _router.SetFallback(ctx =>
        {
            calls.Add("fallback");
            return Task.CompletedTask;
        });

        UpdateDelegate terminal = _router.BuildTerminal();
        UpdateContext ctx = TestHelpers.CreateMessageContext("/start");

        await terminal(ctx);

        calls.Should().Equal("start");
    }
}