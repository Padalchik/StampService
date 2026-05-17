using StampService.TelegramBot.Features.Admin.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Admin.Screens;

public sealed class DemoCustomerCodeScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var view = new ScreenView(
            "<b>Кому создать демо-данные?</b>\n\n" +
            "Введите код пользователя из 4 цифр.")
            .AwaitInput<EnterDemoCustomerCodeAction>()
            .MenuButton("В главное меню");

        return ValueTask.FromResult(view);
    }
}
