using FluentAssertions;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Wizards;

namespace TelegramBotFlow.Core.Tests.Wizards;

public sealed class WizardEdgeCaseTests
{
    // ── Test fixture ───────────────────────────────────────────────────────────

    private sealed class EdgeState
    {
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// A minimal two-step wizard used for edge case testing.
    /// </summary>
    private sealed class EdgeWizard : BotWizard<EdgeState>
    {
        protected override void ConfigureSteps(WizardBuilder<EdgeState> builder)
        {
            builder
                .Step(
                    id: "step1",
                    renderer: (_, _) => new ScreenView("Step 1"),
                    processor: (_, state) =>
                    {
                        state.Value = "from-step1";
                        return Task.FromResult<StepResult>(StepResult.GoTo("step2"));
                    })
                .Step(
                    id: "step2",
                    renderer: (_, _) => new ScreenView("Step 2"),
                    processor: (_, _) => Task.FromResult<StepResult>(StepResult.Finish()));
        }

        public override Task<IEndpointResult> OnFinishedAsync(UpdateContext context, EdgeState state) =>
            Task.FromResult<IEndpointResult>(BotResults.Empty());
    }

    // ── GoBack edge cases ──────────────────────────────────────────────────────

    [Fact]
    public async Task GoBackAsync_EmptyStepHistory_ReturnsIsFinished()
    {
        // When on the first step with no history, GoBack should exit the wizard
        var wizard = new EdgeWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step1",
            StepHistory = []
        };
        UpdateContext ctx = TestHelpers.CreateMessageContext("back");

        WizardTransition transition = await wizard.GoBackAsync(ctx, storageState);

        transition.IsFinished.Should().BeTrue();
        transition.EndpointResult.Should().BeOfType<NavigateBackResult>();
    }

    // ── ProcessUpdateAsync error paths ─────────────────────────────────────────

    [Fact]
    public async Task ProcessUpdateAsync_InvalidStepId_ThrowsInvalidOperationException()
    {
        // If the stored CurrentStepId doesn't exist in the wizard's step registry,
        // ProcessUpdateAsync must throw InvalidOperationException (fail-fast, no silent corruption)
        var wizard = new EdgeWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "nonexistent_step",
            StepHistory = [],
            PayloadJson = "{\"Value\":\"\"}"
        };
        UpdateContext ctx = TestHelpers.CreateMessageContext("hello");

        Func<Task> act = () => wizard.ProcessUpdateAsync(ctx, storageState);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*nonexistent_step*");
    }

    // ── Null/empty PayloadJson ─────────────────────────────────────────────────

    [Fact]
    public async Task ProcessUpdateAsync_NullPayloadJson_CreatesDefaultStateWithoutCrash()
    {
        // An empty/null PayloadJson should create a default TState instead of crashing
        var wizard = new EdgeWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step1",
            StepHistory = [],
            PayloadJson = string.Empty   // no saved state
        };
        UpdateContext ctx = TestHelpers.CreateMessageContext("hello");

        WizardTransition transition = await wizard.ProcessUpdateAsync(ctx, storageState);

        transition.IsFinished.Should().BeFalse();
        storageState.CurrentStepId.Should().Be("step2");
    }
}
