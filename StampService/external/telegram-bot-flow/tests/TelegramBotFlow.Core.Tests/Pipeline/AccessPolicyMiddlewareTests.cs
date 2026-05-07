using NSubstitute;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using IUserAccessPolicy = TelegramBotFlow.Core.Context.IUserAccessPolicy;
using UpdateContext = TelegramBotFlow.Core.Context.UpdateContext;

namespace TelegramBotFlow.Core.Tests.Pipeline;

public sealed class AccessPolicyMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_SetsIsAdminFromPolicy()
    {
        IUserAccessPolicy accessPolicy = Substitute.For<IUserAccessPolicy>();
        var middleware = new AccessPolicyMiddleware(accessPolicy);
        UpdateContext context = TestHelpers.CreateMessageContext("/start");

        accessPolicy.IsAdmin(context).Returns(true);

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.True(context.IsAdmin);
        accessPolicy.Received(1).IsAdmin(context);
    }

    [Fact]
    public async Task InvokeAsync_CallsNext()
    {
        IUserAccessPolicy accessPolicy = Substitute.For<IUserAccessPolicy>();
        var middleware = new AccessPolicyMiddleware(accessPolicy);
        UpdateContext context = TestHelpers.CreateMessageContext("/start");
        bool nextCalled = false;

        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
    }
}