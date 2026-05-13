using StampService.TelegramBot.Features.CoinProducts.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.CoinProducts.Screens;

public sealed class PurchaseCoinProductCodeScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        return ValueTask.FromResult(new ScreenView(
            "<b>Покупка товара за монетки</b>\n\n" +
            "Введите код списания клиента:")
            .AwaitInput<EnterPurchaseCoinProductCodeAction>()
            .BackButton());
    }
}
