using StampService.TelegramBot.Features.Profile.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Profile.Screens;

public sealed class ProfilePhoneNumberScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        return ValueTask.FromResult(new ScreenView(
            "<b>Вход по телефону</b>\n\n" +
            "Введите номер телефона в международном формате, например <code>+79991234567</code>.")
            .AwaitInput<EnterProfilePhoneAction>()
            .BackButton());
    }
}
