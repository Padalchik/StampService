using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using TelegramBotFlow.Core.Sessions;
using TelegramBotFlow.Core.Wizards;
using UpdateContext = TelegramBotFlow.Core.Context.UpdateContext;

namespace TelegramBotFlow.Core.Tests.Pipeline;

public sealed class WizardMiddlewareTests
{
    private readonly IWizardStore _wizardStore;
    private readonly WizardRegistry _wizardRegistry;
    private readonly WizardMiddleware _middleware;

    public WizardMiddlewareTests()
    {
        _wizardStore = Substitute.For<IWizardStore>();
        _wizardRegistry = new WizardRegistry();
        _middleware = new WizardMiddleware(_wizardStore, _wizardRegistry, NullLogger<WizardMiddleware>.Instance);
    }

    private static UserSession CreateSession(long userId = 123) => new(userId);

    [Fact]
    public async Task No_active_wizard_passes_to_next()
    {
        UpdateContext context = TestHelpers.CreateMessageContext("/start");
        var session = CreateSession();
        // ActiveWizardId is null by default
        context.Session = session;

        bool nextCalled = false;
        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        await _wizardStore.DidNotReceive().GetAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Null_session_passes_to_next()
    {
        UpdateContext context = TestHelpers.CreateMessageContext("/start");
        // context.Session is null by default (not set)

        bool nextCalled = false;
        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        await _wizardStore.DidNotReceive().GetAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancel_command_clears_active_wizard_id_deletes_state_and_passes_to_next()
    {
        UpdateContext context = TestHelpers.CreateMessageContext("/cancel");
        var session = CreateSession();
        session.Navigation.ActiveWizardId = "TestWizard";
        context.Session = session;

        bool nextCalled = false;
        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.Null(context.Session!.Navigation.ActiveWizardId);
        await _wizardStore.Received(1).DeleteAsync(context.UserId, "TestWizard", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NavMenu_callback_clears_active_wizard_id_deletes_state_and_passes_to_next()
    {
        UpdateContext context = TestHelpers.CreateCallbackContext("nav:menu");
        var session = CreateSession();
        session.Navigation.ActiveWizardId = "TestWizard";
        context.Session = session;

        bool nextCalled = false;
        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.Null(context.Session!.Navigation.ActiveWizardId);
        await _wizardStore.Received(1).DeleteAsync(context.UserId, "TestWizard", Arg.Any<CancellationToken>());
    }
}
