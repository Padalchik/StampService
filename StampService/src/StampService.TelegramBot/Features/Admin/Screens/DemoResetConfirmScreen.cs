using StampService.TelegramBot.Features.Admin.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Admin.Screens;

public sealed class DemoResetConfirmScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var view = new ScreenView(
            "<b>Очистить всю БД?</b>\n\n" +
            "Будут удалены пользователи, бренды, балансы, транзакции, товары, метрики и настройки. " +
            "Операцию нельзя отменить.")
            .Button<ConfirmDemoResetAction>("Да, очистить")
            .Row()
            .Button<CancelDemoResetAction>("Отмена");

        return ValueTask.FromResult(view);
    }
}
