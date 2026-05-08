using StampService.Application.Abstractions;
using StampService.Application.Users.Commands.CreateRedemptionCode;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Users;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.RedemptionCode.Screens;

public sealed class MyRedemptionCodeScreen : IScreen
{
    private readonly ICommandHandler<CreateRedemptionCodeResponse, CreateRedemptionCodeCommand> _createCodeHandler;
    private readonly ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> _ensureUserHandler;

    public MyRedemptionCodeScreen(
        ICommandHandler<CreateRedemptionCodeResponse, CreateRedemptionCodeCommand> createCodeHandler,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler)
    {
        _createCodeHandler = createCodeHandler;
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
            return new ScreenView("Не удалось определить пользователя.")
                .BackButton();
        }

        var codeResult = await _createCodeHandler.Handle(
            new CreateRedemptionCodeCommand(userResult.Value.UserId),
            ctx.CancellationToken);

        if (codeResult.IsFailed)
        {
            return new ScreenView("Не удалось создать код для списания.")
                .BackButton();
        }

        return new ScreenView(
            "<b>Код для списания</b>\n\n" +
            $"<code>{codeResult.Value.Code}</code>\n\n" +
            $"Действует до: {codeResult.Value.ExpiresAtUtc:HH:mm:ss} UTC\n\n" +
            "Покажите этот код сотруднику только для списания метрики. Код одноразовый.")
            .BackButton();
    }
}
