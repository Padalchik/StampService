using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Common.Routing;

public static class BotEndpointHelpers
{
    public static Task<Result<EnsureTelegramUserResponse>> EnsureUserAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler)
    {
        var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
        return ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(
                ctx.UserId,
                from?.FirstName,
                from?.LastName,
                from?.Username),
            ctx.CancellationToken);
    }

    public static Task<IEndpointResult> RetryInput<TScreen, TAction>(string message)
        where TScreen : IScreen
        where TAction : IBotAction
    {
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(message)
            .AwaitInput<TAction>()
            .BackButton())));
    }

    public static IEndpointResult ErrorView(string message)
    {
        return BotResults.ShowView(new ScreenView(message).BackButton());
    }
}
