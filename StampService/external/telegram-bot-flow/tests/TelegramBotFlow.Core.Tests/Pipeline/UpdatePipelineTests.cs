using FluentAssertions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Pipeline;

namespace TelegramBotFlow.Core.Tests.Pipeline;

public sealed class UpdatePipelineTests
{
    [Fact]
    public async Task ProcessAsync_WithNoMiddlewares_CallsTerminal()
    {
        bool terminalCalled = false;

        var pipeline = UpdatePipeline.Build([], _ =>
        {
            terminalCalled = true;
            return Task.CompletedTask;
        });

        UpdateContext ctx = TestHelpers.CreateMessageContext("/start");

        await pipeline.ProcessAsync(ctx);

        terminalCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessAsync_MiddlewaresExecuteInOrder()
    {
        var order = new List<int>();

        Func<UpdateDelegate, UpdateDelegate> middleware1 = next => async ctx =>
        {
            order.Add(1);
            await next(ctx);
        };

        Func<UpdateDelegate, UpdateDelegate> middleware2 = next => async ctx =>
        {
            order.Add(2);
            await next(ctx);
        };

        var pipeline = UpdatePipeline.Build([middleware1, middleware2], _ =>
        {
            order.Add(3);
            return Task.CompletedTask;
        });

        UpdateContext ctx = TestHelpers.CreateMessageContext("/test");

        await pipeline.ProcessAsync(ctx);

        order.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task ProcessAsync_MiddlewareCanShortCircuit()
    {
        bool terminalCalled = false;

        Func<UpdateDelegate, UpdateDelegate> shortCircuit = _ => _ => Task.CompletedTask;

        var pipeline = UpdatePipeline.Build([shortCircuit], _ =>
        {
            terminalCalled = true;
            return Task.CompletedTask;
        });

        UpdateContext ctx = TestHelpers.CreateMessageContext("/test");

        await pipeline.ProcessAsync(ctx);

        terminalCalled.Should().BeFalse();
    }
}