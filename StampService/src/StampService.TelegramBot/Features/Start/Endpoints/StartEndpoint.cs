using StampService.Application.Abstractions;
using StampService.Application.Users.Commands.ConfirmTelegramLinkSession;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Profile;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Features.MainMenu.Screens;
using TelegramBotFlow.Core.Constants;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;

namespace StampService.TelegramBot.Features.Start.Endpoints;

public sealed class StartEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapDeepLink(BotCommands.START, async (
            UpdateContext ctx,
            ICommandHandler<ConfirmTelegramLinkResponse, ConfirmTelegramLinkSessionCommand> linkHandler) =>
        {
            var payload = ctx.CommandArgument ?? string.Empty;
            if (!payload.StartsWith("l_", StringComparison.Ordinal))
                return BotResults.ShowView(new("Ссылка привязки устарела или некорректна."));

            var from = ctx.Update.Message?.From;
            var result = await linkHandler.Handle(
                new ConfirmTelegramLinkSessionCommand(
                    payload,
                    ctx.UserId,
                    from?.FirstName,
                    from?.LastName,
                    from?.Username),
                ctx.CancellationToken);

            if (result.IsFailed)
                return BotResults.ShowView(new(
                    $"Не удалось привязать Telegram: {BotErrorFormatter.Format(result.Errors)}."));

            ctx.Session?.Clear();
            return BotResults.ShowView(new(
                "<b>Telegram привязан</b>\n\n" +
                "Теперь этот Telegram связан с вашим профилем StampService."));
        });

        app.MapCommand(BotCommands.START, async (
            UpdateContext ctx,
            ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> handler) =>
        {
            var from = ctx.Update.Message?.From;
            var result = await handler.Handle(
                new EnsureTelegramUserCommand(
                    ctx.UserId,
                    from?.FirstName,
                    from?.LastName,
                    from?.Username),
                ctx.CancellationToken);

            if (result.IsFailed)
                return BotResults.ShowView(new("Не удалось авторизоваться в StampService."));

            ctx.Session?.Clear();
            return BotResults.NavigateToRoot<MainMenuScreen>();
        });
    }
}
