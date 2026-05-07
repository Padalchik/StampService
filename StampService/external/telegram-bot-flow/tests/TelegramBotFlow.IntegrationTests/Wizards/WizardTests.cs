using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Wizards;
using TelegramBotFlow.IntegrationTests.Infrastructure;

namespace TelegramBotFlow.IntegrationTests.Wizards;

[Collection(nameof(BotApplicationTests))]
public class WizardTests
{
    private readonly BotWebApplicationFactory _factory;
    private IServiceScope _scope;

    public WizardTests(BotWebApplicationFactory factory)
    {
        _factory = factory;
        _scope = _factory.Services.CreateScope();
    }

    private async Task SendUpdateAsync(Update update)
    {
        var pipeline = _factory.Services.GetRequiredService<UpdatePipeline>();
        var context = new UpdateContext(update, _scope.ServiceProvider);
        try
        {
            await pipeline.ProcessAsync(context);
        }
        catch (Exception ex)
        {
            throw new Exception($"Pipeline error on update {update.Id}: {ex.Message}", ex);
        }
    }

    [Fact]
    public async Task Wizard_Should_Process_Steps_And_Finish()
    {
        // 1. Arrange - start wizard
        var startWizardUpdate = new Update
        {
            Id = 1,
            Message = new Message
            {
                Id = 100,
                Date = DateTime.UtcNow,
                Text = "/testwizard",
                Chat = new Chat { Id = 12345, Type = Telegram.Bot.Types.Enums.ChatType.Private },
                From = new User { Id = 12345, IsBot = false, FirstName = "Test" }
            }
        };

        // Act 1 - trigger wizard
        var ctx = new UpdateContext(startWizardUpdate, _scope.ServiceProvider);
        var sessionStore = _scope.ServiceProvider.GetRequiredService<TelegramBotFlow.Core.Sessions.ISessionStore>();
        ctx.Session = await sessionStore.GetOrCreateAsync(12345, CancellationToken.None);
        await ctx.StartWizardAsync<TestWizard>(ctx.CancellationToken);
        await sessionStore.SaveAsync(ctx.Session, CancellationToken.None);

        // Assert 1 - wizard started, user in active wizard
        var store = _scope.ServiceProvider.GetRequiredService<IWizardStore>();
        var state = await store.GetAsync(12345, nameof(TestWizard), CancellationToken.None);

        state.Should().NotBeNull();
        state!.CurrentStepId.Should().Be("step1");

        // Act 2 - send age (first step)
        var step1Update = new Update
        {
            Id = 2,
            Message = new Message
            {
                Id = 101,
                Date = DateTime.UtcNow,
                Text = "25",
                Chat = new Chat { Id = 12345, Type = Telegram.Bot.Types.Enums.ChatType.Private },
                From = new User { Id = 12345, IsBot = false, FirstName = "Test" }
            }
        };
        await SendUpdateAsync(step1Update);

        // Assert 2 - moved to step 2
        state = await store.GetAsync(12345, nameof(TestWizard), CancellationToken.None);
        state.Should().NotBeNull();
        state!.CurrentStepId.Should().Be("step2");
        state.PayloadJson.Should().Contain("\"Age\":25");

        // Act 3 - send name (second step)
        var step2Update = new Update
        {
            Id = 3,
            Message = new Message
            {
                Id = 102,
                Date = DateTime.UtcNow,
                Text = "John Doe",
                Chat = new Chat { Id = 12345, Type = Telegram.Bot.Types.Enums.ChatType.Private },
                From = new User { Id = 12345, IsBot = false, FirstName = "Test" }
            }
        };
        await SendUpdateAsync(step2Update);

        // Assert 3 - wizard finished and state removed
        state = await store.GetAsync(12345, nameof(TestWizard), CancellationToken.None);
        state.Should().BeNull();
    }
}

// Демо-состояние
public class TestWizardState
{
    public int Age { get; set; }
    public string Name { get; set; } = string.Empty;
}

// Демо-визард
public class TestWizard : BotWizard<TestWizardState>
{
    protected override void ConfigureSteps(WizardBuilder<TestWizardState> builder)
    {
        builder
            .Step(
                id: "step1",
                renderer: (ctx, state) => new ScreenView("Enter your age:"),
                processor: async (ctx, state) =>
                {
                    if (int.TryParse(ctx.MessageText, out int age))
                    {
                        state.Age = age;
                        return StepResult.GoTo("step2");
                    }

                    return StepResult.Stay("Please enter a valid number");
                }
            )
            .Step(
                id: "step2",
                renderer: (ctx, state) => new ScreenView("Enter your name:"),
                processor: async (ctx, state) =>
                {
                    if (string.IsNullOrWhiteSpace(ctx.MessageText))
                        return StepResult.Stay("Name cannot be empty");

                    state.Name = ctx.MessageText;
                    return StepResult.Finish();
                }
            );
    }

    public override Task<IEndpointResult> OnFinishedAsync(TelegramBotFlow.Core.Context.UpdateContext context,
        TestWizardState state)
    {
        // Завершение визарда - возврат пустого результата (для теста)
        return Task.FromResult<IEndpointResult>(BotResults.Empty());
    }
}