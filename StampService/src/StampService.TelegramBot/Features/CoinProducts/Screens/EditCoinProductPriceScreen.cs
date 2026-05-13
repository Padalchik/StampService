using StampService.TelegramBot.Features.CoinProducts.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.CoinProducts.Screens;

public sealed class EditCoinProductPriceScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var current = ctx.Session?.Data.Get<int>(CoinProductSessionKeys.SelectedProductPrice) ?? 0;
        return ValueTask.FromResult(new ScreenView(
            "<b>Редактирование товара</b>\n\n" +
            $"Текущая цена: {current} монеток\n" +
            "Введите новую цену:")
            .AwaitInput<EnterEditCoinProductPriceAction>()
            .Button<KeepEditCoinProductPriceAction>("Оставить эти данные")
            .BackButton());
    }
}
