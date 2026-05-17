using StampService.TelegramBot.Features.Admin.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Admin.Screens;

public sealed class AdminDemoScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var view = new ScreenView(
            "<b>Работа с демо</b>\n\n" +
            "Здесь можно очистить базу или быстро создать пользователю демонстрационные балансы, награды и историю.")
            .Button<CreateDemoBrandsAction>("Создать демо-бренды")
            .Row()
            .Button<StartCreateUserDemoDataAction>("Создать данные пользователю")
            .Row()
            .Button<StartDemoResetAction>("Очистить всю БД")
            .Row()
            .MenuButton("В главное меню");

        return ValueTask.FromResult(view);
    }
}
