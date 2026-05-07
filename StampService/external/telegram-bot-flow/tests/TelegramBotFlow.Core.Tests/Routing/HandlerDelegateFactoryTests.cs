using FluentAssertions;
using NSubstitute;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Tests.Routing;

public sealed class HandlerDelegateFactoryTests
{
    // -- Helpers --

    private static IServiceProvider BuildServices(INavigationService? navigator = null)
    {
        IServiceProvider services = Substitute.For<IServiceProvider>();
        INavigationService nav = navigator ?? Substitute.For<INavigationService>();
        IUpdateResponder responder = Substitute.For<IUpdateResponder>();
        services.GetService(typeof(INavigationService)).Returns(nav);
        services.GetService(typeof(IUpdateResponder)).Returns(responder);
        return services;
    }

    private static UpdateContext CreateMessageContext(IServiceProvider? services = null)
        => TestHelpers.CreateMessageContext("test", services: services ?? BuildServices());

    private static UpdateContext CreateCallbackContext(string data, IServiceProvider? services = null)
        => TestHelpers.CreateCallbackContext(data, services: services ?? BuildServices());

    private static UpdateContext WithSession(UpdateContext ctx)
    {
        ctx.Session = new UserSession(ctx.UserId);
        return ctx;
    }

    // -- Create: validation --

    [Fact]
    public void Create_Should_Throw_When_Handler_Returns_Task()
    {
        Delegate handler = () => Task.CompletedTask;

        Action act = () => HandlerDelegateFactory.Create(handler);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Task<IEndpointResult>*");
    }

    [Fact]
    public void Create_Should_Throw_When_Handler_Returns_Void()
    {
        Delegate handler = () => { };

        Action act = () => HandlerDelegateFactory.Create(handler);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Task<IEndpointResult>*");
    }

    [Fact]
    public void Create_Should_Not_Throw_When_Handler_Returns_TaskEndpointResult()
    {
        Delegate handler = () => Task.FromResult(BotResults.Empty());

        Action act = () => HandlerDelegateFactory.Create(handler);

        act.Should().NotThrow();
    }

    // -- Create: execution --

    [Fact]
    public async Task Create_Should_Execute_Result_When_Handler_Returns_EndpointResult()
    {
        var result = Substitute.For<IEndpointResult>();
        Delegate handler = () => Task.FromResult(result);

        UpdateDelegate del = HandlerDelegateFactory.Create(handler);
        UpdateContext ctx = CreateMessageContext();

        await del(ctx);

        await result.Received(1).ExecuteAsync(Arg.Is<BotExecutionContext>(c => c.Update == ctx));
    }

    [Fact]
    public async Task Create_Should_Inject_Services_Into_Handler_Parameters()
    {
        var capturedService = (IUpdateResponder?)null;
        var responder = Substitute.For<IUpdateResponder>();
        IServiceProvider services = BuildServices();
        services.GetService(typeof(IUpdateResponder)).Returns(responder);

        Delegate handler = (IUpdateResponder svc) =>
        {
            capturedService = svc;
            return Task.FromResult(BotResults.Empty());
        };

        UpdateDelegate del = HandlerDelegateFactory.Create(handler);
        UpdateContext ctx = CreateMessageContext(services);

        await del(ctx);

        capturedService.Should().BeSameAs(responder);
    }

    // -- CreateForInput --

    [Fact]
    public async Task CreateForInput_ClearsPendingActionId_BeforeInvocation()
    {
        string? pendingAtInvocation = "not-cleared";

        Delegate handler = (UpdateContext ctx) =>
        {
            pendingAtInvocation = ctx.Session?.Navigation.PendingInputActionId;
            return Task.FromResult(BotResults.Empty());
        };

        UpdateDelegate del = HandlerDelegateFactory.CreateForInput(handler, "my-action");
        UpdateContext ctx = WithSession(CreateMessageContext());
        ctx.Session!.Navigation.SetPending("my-action");

        await del(ctx);

        pendingAtInvocation.Should().BeNull("pending should be cleared before handler runs");
    }

    [Fact]
    public async Task CreateForInput_PassesPendingActionIdToExecutionContext()
    {
        BotExecutionContext? capturedCtx = null;

        var result = Substitute.For<IEndpointResult>();
        result.ExecuteAsync(Arg.Any<BotExecutionContext>())
            .Returns(c =>
            {
                capturedCtx = c.Arg<BotExecutionContext>();
                return Task.CompletedTask;
            });

        Delegate handler = () => Task.FromResult(result);
        UpdateDelegate del = HandlerDelegateFactory.CreateForInput(handler, "my-action");
        UpdateContext ctx = WithSession(CreateMessageContext());

        await del(ctx);

        capturedCtx.Should().NotBeNull();
        capturedCtx!.PendingActionId.Should().Be("my-action");
    }

    [Fact]
    public async Task CreateForInput_StayResult_RestoresPendingAction()
    {
        Delegate handler = () => Task.FromResult(BotResults.Stay());
        UpdateDelegate del = HandlerDelegateFactory.CreateForInput(handler, "my-action");
        UpdateContext ctx = WithSession(CreateMessageContext());
        ctx.Session!.Navigation.SetPending("my-action");

        await del(ctx);

        ctx.Session.Navigation.PendingInputActionId.Should().Be("my-action");
    }

    [Fact]
    public async Task CreateForInput_OtherResult_LeavesPendingCleared()
    {
        Delegate handler = () => Task.FromResult(BotResults.Empty());
        UpdateDelegate del = HandlerDelegateFactory.CreateForInput(handler, "my-action");
        UpdateContext ctx = WithSession(CreateMessageContext());
        ctx.Session!.Navigation.SetPending("my-action");

        await del(ctx);

        ctx.Session.Navigation.PendingInputActionId.Should().BeNull();
    }

    // -- CreateForCallbackGroup --

    [Fact]
    public void CreateForCallbackGroup_Should_Throw_When_Handler_Returns_Void()
    {
        Delegate handler = (string action) => { };

        Action act = () => HandlerDelegateFactory.CreateForCallbackGroup(handler, "nav");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Task<IEndpointResult>*");
    }

    [Fact]
    public async Task CreateForCallbackGroup_PassesActionPartToFirstStringParameter()
    {
        string? capturedAction = null;

        Delegate handler = (string action) =>
        {
            capturedAction = action;
            return Task.FromResult(BotResults.Empty());
        };

        UpdateDelegate del = HandlerDelegateFactory.CreateForCallbackGroup(handler, "nav");
        UpdateContext ctx = CreateCallbackContext("nav:back");

        await del(ctx);

        capturedAction.Should().Be("back");
    }
}