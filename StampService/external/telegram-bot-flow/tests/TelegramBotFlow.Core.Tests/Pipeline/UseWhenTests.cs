using FluentAssertions;
using TelegramBotFlow.Core.Pipeline;

namespace TelegramBotFlow.Core.Tests.Pipeline;

public sealed class UseWhenTests
{
    [Fact]
    public async Task UseWhen_PredicateTrue_ExecutesBranch()
    {
        bool branchExecuted = false;
        Func<UpdateDelegate, UpdateDelegate> branchMiddleware = next => async ctx =>
        {
            branchExecuted = true;
            await next(ctx);
        };

        var conditional = ConditionalMiddleware.Create(_ => true, [branchMiddleware]);
        var pipeline = UpdatePipeline.Build([conditional], _ => Task.CompletedTask);
        await pipeline.ProcessAsync(TestHelpers.CreateMessageContext("hello"));

        branchExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task UseWhen_PredicateFalse_SkipsBranch()
    {
        bool branchExecuted = false;
        Func<UpdateDelegate, UpdateDelegate> branchMiddleware = next => async ctx =>
        {
            branchExecuted = true;
            await next(ctx);
        };

        var conditional = ConditionalMiddleware.Create(_ => false, [branchMiddleware]);
        var pipeline = UpdatePipeline.Build([conditional], _ => Task.CompletedTask);
        await pipeline.ProcessAsync(TestHelpers.CreateMessageContext("hello"));

        branchExecuted.Should().BeFalse();
    }

    [Fact]
    public async Task UseWhen_PredicateFalse_StillCallsNext()
    {
        bool terminalReached = false;
        var conditional = ConditionalMiddleware.Create(_ => false, []);
        var pipeline = UpdatePipeline.Build([conditional], _ =>
        {
            terminalReached = true;
            return Task.CompletedTask;
        });
        await pipeline.ProcessAsync(TestHelpers.CreateMessageContext("hello"));

        terminalReached.Should().BeTrue();
    }
}
