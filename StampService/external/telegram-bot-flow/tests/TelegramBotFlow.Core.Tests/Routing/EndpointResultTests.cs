using FluentAssertions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Sessions;
using TelegramBotFlow.Core.Wizards;

namespace TelegramBotFlow.Core.Tests.Routing;

public sealed class EndpointResultTests
{
    // -- Helpers --

    private static BotExecutionContext CreateCallbackCtx(string? pendingActionId = null)
    {
        var update = new UpdateContext(
            new Update
            {
                CallbackQuery = new CallbackQuery
                {
                    Id = "cb-1",
                    Data = "test",
                    From = new User { Id = 1, FirstName = "T" },
                    Message = new Message
                    {
                        Id = 10,
                        Date = DateTime.UtcNow,
                        Chat = new Chat { Id = 1, Type = ChatType.Private },
                        From = new User { Id = 999, IsBot = true, FirstName = "Bot" }
                    }
                }
            },
            Substitute.For<IServiceProvider>());

        update.Session = new UserSession(1);

        return new BotExecutionContext(
            update,
            navigator: Substitute.For<INavigationService>(),
            responder: Substitute.For<IUpdateResponder>(),
            pendingActionId: pendingActionId);
    }

    private static BotExecutionContext CreateMessageCtx(string? pendingActionId = null)
    {
        var update = new UpdateContext(
            new Update
            {
                Message = new Message
                {
                    Id = 1,
                    Text = "hello",
                    Date = DateTime.UtcNow,
                    Chat = new Chat { Id = 1, Type = ChatType.Private },
                    From = new User { Id = 1, FirstName = "T" }
                }
            },
            Substitute.For<IServiceProvider>());

        update.Session = new UserSession(1);

        return new BotExecutionContext(
            update,
            navigator: Substitute.For<INavigationService>(),
            responder: Substitute.For<IUpdateResponder>(),
            pendingActionId: pendingActionId);
    }

    // -- NavigateToResult --

    [Fact]
    public async Task NavigateToResult_AnswersCallback_ThenNavigates()
    {
        var ctx = CreateCallbackCtx();
        var result = new NavigateToResult(typeof(object));

        await result.ExecuteAsync(ctx);

        await ctx.Responder.Received(1).AnswerCallbackAsync(ctx.Update, null);
        await ctx.Navigator.Received(1).NavigateToAsync(ctx.Update, typeof(object));
    }

    [Fact]
    public async Task NavigateToResult_SkipsAnswerCallback_WhenNoCallbackQuery()
    {
        var ctx = CreateMessageCtx();
        var result = new NavigateToResult(typeof(object));

        await result.ExecuteAsync(ctx);

        await ctx.Responder.DidNotReceive().AnswerCallbackAsync(ctx.Update, Arg.Any<string>());
        await ctx.Navigator.Received(1).NavigateToAsync(ctx.Update, typeof(object));
    }

    // -- NavigateBackResult --

    [Fact]
    public async Task NavigateBackResult_AnswersCallback_ThenNavigatesBack()
    {
        var ctx = CreateCallbackCtx();
        var result = new NavigateBackResult();

        await result.ExecuteAsync(ctx);

        await ctx.Responder.Received(1).AnswerCallbackAsync(ctx.Update, null);
        await ctx.Navigator.Received(1).NavigateBackAsync(ctx.Update);
    }

    [Fact]
    public async Task NavigateBackResult_WithNotification_PassesTextToAnswerCallback()
    {
        var ctx = CreateCallbackCtx();
        var result = new NavigateBackResult("✅ Сохранено");

        await result.ExecuteAsync(ctx);

        await ctx.Responder.Received(1).AnswerCallbackAsync(ctx.Update, "✅ Сохранено");
        await ctx.Navigator.Received(1).NavigateBackAsync(ctx.Update);
    }

    // -- RefreshResult --

    [Fact]
    public async Task RefreshResult_AnswersCallback_ThenRefreshes()
    {
        var ctx = CreateCallbackCtx();
        var result = new RefreshResult();

        await result.ExecuteAsync(ctx);

        await ctx.Responder.Received(1).AnswerCallbackAsync(ctx.Update, null);
        await ctx.Navigator.Received(1).RefreshScreenAsync(ctx.Update);
    }

    [Fact]
    public async Task RefreshResult_WithNotification_PassesTextToAnswerCallback()
    {
        var ctx = CreateCallbackCtx();
        var result = new RefreshResult("🔄 Обновлено");

        await result.ExecuteAsync(ctx);

        await ctx.Responder.Received(1).AnswerCallbackAsync(ctx.Update, "🔄 Обновлено");
        await ctx.Navigator.Received(1).RefreshScreenAsync(ctx.Update);
    }

    // -- NavigateToRootResult --

    [Fact]
    public async Task NavigateToRootResult_AnswersCallback_ThenNavigatesToRoot()
    {
        var ctx = CreateCallbackCtx();
        var result = new NavigateToRootResult(typeof(object));

        await result.ExecuteAsync(ctx);

        await ctx.Responder.Received(1).AnswerCallbackAsync(ctx.Update, null);
        await ctx.Navigator.Received(1).NavigateToRootAsync(ctx.Update, typeof(object));
    }

    // -- NavigateToByIdResult --

    [Fact]
    public async Task NavigateToByIdResult_AnswersCallback_ThenNavigatesById()
    {
        var ctx = CreateCallbackCtx();
        var result = new NavigateToByIdResult("settings");

        await result.ExecuteAsync(ctx);

        await ctx.Responder.Received(1).AnswerCallbackAsync(ctx.Update, null);
        await ctx.Navigator.Received(1).NavigateToAsync(ctx.Update, "settings");
    }

    // -- NavigateToRootByIdResult --

    [Fact]
    public async Task NavigateToRootByIdResult_AnswersCallback_ThenNavigatesToRootById()
    {
        var ctx = CreateCallbackCtx();
        var result = new NavigateToRootByIdResult("main");

        await result.ExecuteAsync(ctx);

        await ctx.Responder.Received(1).AnswerCallbackAsync(ctx.Update, null);
        await ctx.Navigator.Received(1).NavigateToRootAsync(ctx.Update, "main");
    }

    // -- ShowViewResult --

    [Fact]
    public async Task ShowViewResult_AnswersCallback_ThenShowsView()
    {
        var ctx = CreateCallbackCtx();
        var view = new ScreenView("text");
        var result = new ShowViewResult(view);

        await result.ExecuteAsync(ctx);

        await ctx.Responder.Received(1).AnswerCallbackAsync(ctx.Update, null);
        await ctx.Navigator.Received(1).ShowViewAsync(ctx.Update, view);
    }

    // -- EmptyResult --

    [Fact]
    public async Task EmptyResult_AnswersCallback_AndDoesNothingElse()
    {
        var ctx = CreateCallbackCtx();

        await EmptyResult.Instance.ExecuteAsync(ctx);

        await ctx.Responder.Received(1).AnswerCallbackAsync(ctx.Update, null);
        await ctx.Navigator.DidNotReceiveWithAnyArgs().NavigateToAsync(ctx.Update, (string)null!);
        await ctx.Navigator.DidNotReceiveWithAnyArgs().NavigateBackAsync(ctx.Update);
    }

    // -- AnswerCallbackResult --

    [Fact]
    public async Task AnswerCallbackResult_AnswersCallbackWithText_AndDoesNothingElse()
    {
        var ctx = CreateCallbackCtx();
        var result = new AnswerCallbackResult("👍 Готово");

        await result.ExecuteAsync(ctx);

        await ctx.Responder.Received(1).AnswerCallbackAsync(ctx.Update, "👍 Готово");
        await ctx.Navigator.DidNotReceiveWithAnyArgs().NavigateToAsync(ctx.Update, (string)null!);
    }

    [Fact]
    public async Task AnswerCallbackResult_WithNullText_AnswersCallbackSilently()
    {
        var ctx = CreateCallbackCtx();
        var result = new AnswerCallbackResult();

        await result.ExecuteAsync(ctx);

        await ctx.Responder.Received(1).AnswerCallbackAsync(ctx.Update, null);
    }

    // -- StayResult --

    [Fact]
    public async Task StayResult_RestoresPendingActionId_WhenPendingActionIdSet()
    {
        var ctx = CreateMessageCtx(pendingActionId: "my-action");

        await new StayResult().ExecuteAsync(ctx);

        ctx.Update.Session!.Navigation.PendingInputActionId.Should().Be("my-action");
    }

    [Fact]
    public async Task StayResult_DoesNotRestorePending_WhenNoPendingActionId()
    {
        var ctx = CreateMessageCtx(pendingActionId: null);

        await new StayResult().ExecuteAsync(ctx);

        ctx.Update.Session!.Navigation.PendingInputActionId.Should().BeNull();
    }

    [Fact]
    public async Task StayResult_DeletesMessage_WhenDeleteMessageTrue()
    {
        var ctx = CreateMessageCtx();

        await new StayResult(DeleteMessage: true).ExecuteAsync(ctx);

        await ctx.Responder.Received(1).DeleteMessageAsync(ctx.Update);
    }

    [Fact]
    public async Task StayResult_DoesNotDeleteMessage_WhenDeleteMessageFalse()
    {
        var ctx = CreateMessageCtx();

        await new StayResult(DeleteMessage: false).ExecuteAsync(ctx);

        await ctx.Responder.DidNotReceive().DeleteMessageAsync(ctx.Update);
    }

    [Fact]
    public async Task StayResult_WithNotification_AnswersCallback()
    {
        var ctx = CreateMessageCtx(pendingActionId: "my-action");

        await new StayResult("❌ Неверно").ExecuteAsync(ctx);

        await ctx.Responder.Received(1).AnswerCallbackAsync(ctx.Update, "❌ Неверно");
    }

    [Fact]
    public async Task StayResult_WithoutNotification_DoesNotAnswerCallback()
    {
        var ctx = CreateMessageCtx(pendingActionId: "my-action");

        await new StayResult().ExecuteAsync(ctx);

        await ctx.Responder.DidNotReceive().AnswerCallbackAsync(Arg.Any<UpdateContext>(), Arg.Any<string>());
    }

    // -- NavigateToResult.WithArg --

    [Fact]
    public async Task NavigateToResult_WithArg_PopulatesNavArgBeforeNavigation()
    {
        // Arrange
        string? capturedArg = null;
        var ctx = CreateCallbackCtx();
        ctx.Navigator
            .When(n => n.NavigateToAsync(Arg.Any<UpdateContext>(), Arg.Any<Type>()))
            .Do(_ => capturedArg = ctx.Update.Session?.Navigation.GetNavigationArg("userId"));

        var result = new NavigateToResult(typeof(object)).WithArg("userId", "42");

        // Act
        await result.ExecuteAsync(ctx);

        // Assert
        capturedArg.Should().Be("42");
    }

    [Fact]
    public async Task NavigateToResult_ChainedWithArgs_AccumulatesAllArgs()
    {
        var ctx = CreateCallbackCtx();
        var result = new NavigateToResult(typeof(object))
            .WithArg("userId", "42")
            .WithArg("mode", "edit");

        await result.ExecuteAsync(ctx);

        ctx.Update.Session?.Navigation.GetNavigationArg("userId").Should().Be("42");
        ctx.Update.Session?.Navigation.GetNavigationArg("mode").Should().Be("edit");
    }

    [Fact]
    public void NavigateToResult_WithArg_ReturnsNewInstance_PreservingScreenType()
    {
        var original = new NavigateToResult(typeof(object));
        var modified = original.WithArg("key", "value");

        modified.Should().NotBeSameAs(original);
        modified.ScreenType.Should().Be(typeof(object));
        modified.NavArgs.Should().ContainKey("key").WhoseValue.Should().Be("value");
    }

    [Fact]
    public void NavigateToResult_WithArg_Generic_SerializesValue()
    {
        var result = new NavigateToResult(typeof(object)).WithArg("count", 5);

        result.NavArgs.Should().ContainKey("count").WhoseValue.Should().Be("5");
    }

    [Fact]
    public async Task NavigateToByIdResult_WithArg_PopulatesNavArg()
    {
        var ctx = CreateCallbackCtx();
        var result = new NavigateToByIdResult("profile").WithArg("section", "bio");

        await result.ExecuteAsync(ctx);

        ctx.Update.Session?.Navigation.GetNavigationArg("section").Should().Be("bio");
    }

    [Fact]
    public async Task NavigateToRootResult_WithArg_PopulatesNavArg()
    {
        var ctx = CreateCallbackCtx();
        var result = new NavigateToRootResult(typeof(object)).WithArg("reset", "true");

        await result.ExecuteAsync(ctx);

        ctx.Update.Session?.Navigation.GetNavigationArg("reset").Should().Be("true");
    }

    [Fact]
    public async Task NavigateToRootByIdResult_WithArg_PopulatesNavArg()
    {
        var ctx = CreateCallbackCtx();
        var result = new NavigateToRootByIdResult("home").WithArg("welcome", "1");

        await result.ExecuteAsync(ctx);

        ctx.Update.Session?.Navigation.GetNavigationArg("welcome").Should().Be("1");
    }

    [Fact]
    public async Task NavigateToResult_WithoutArgs_NavigatesNormally()
    {
        var ctx = CreateCallbackCtx();
        var result = new NavigateToResult(typeof(object));

        await result.ExecuteAsync(ctx);

        await ctx.Navigator.Received(1).NavigateToAsync(ctx.Update, typeof(object));
    }

    // -- StartWizardResult --

    [Fact]
    public async Task StartWizardResult_ThrowsWhenWizardsNotRegistered()
    {
        var ctx = CreateCallbackCtx();
        // ctx.Wizards is null

        Func<Task> act = () => new StartWizardResult(typeof(object)).ExecuteAsync(ctx);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*AddWizards*");
    }

    [Fact]
    public async Task StartWizardResult_ExecutesFirstStepResult_WhenWizardLaunches()
    {
        var ctx = CreateCallbackCtx();
        var wizards = Substitute.For<IWizardLauncher>();
        var firstStepResult = Substitute.For<IEndpointResult>();
        wizards.LaunchAsync(typeof(object), ctx.Update).Returns(firstStepResult);

        var execCtx = new BotExecutionContext(ctx.Update, ctx.Navigator, ctx.Responder, wizards);
        await new StartWizardResult(typeof(object)).ExecuteAsync(execCtx);

        await firstStepResult.Received(1).ExecuteAsync(execCtx);
    }

    [Fact]
    public async Task StartWizardResult_AnswersCallback_WhenNoFirstStepResult()
    {
        var ctx = CreateCallbackCtx();
        var wizards = Substitute.For<IWizardLauncher>();
        wizards.LaunchAsync(typeof(object), ctx.Update).Returns((IEndpointResult?)null);

        var execCtx = new BotExecutionContext(ctx.Update, ctx.Navigator, ctx.Responder, wizards);
        await new StartWizardResult(typeof(object)).ExecuteAsync(execCtx);

        await execCtx.Responder.Received(1).AnswerCallbackAsync(execCtx.Update, null);
    }
}