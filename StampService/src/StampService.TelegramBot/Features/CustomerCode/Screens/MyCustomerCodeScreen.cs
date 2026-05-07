using StampService.Application.Abstractions;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.CustomerCode.Screens;

public sealed class MyCustomerCodeScreen : IScreen
{
    private readonly ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> _ensureUserHandler;

    public MyCustomerCodeScreen(
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler)
    {
        _ensureUserHandler = ensureUserHandler;
    }

    public async ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
        var userResult = await _ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(
                ctx.UserId,
                from?.FirstName,
                from?.LastName,
                from?.Username),
            ctx.CancellationToken);

        if (userResult.IsFailed)
        {
            return new ScreenView("Не удалось получить клиентский код.")
                .BackButton();
        }

        return new ScreenView(
            "<b>Мой клиентский код</b>\n\n" +
            $"<code>{userResult.Value.CustomerCode}</code>\n\n" +
            "Покажите этот код сотруднику, чтобы он выдал метрику.")
            .BackButton();
    }
}
