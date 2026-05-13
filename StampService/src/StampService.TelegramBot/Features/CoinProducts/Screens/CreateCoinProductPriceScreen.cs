using StampService.TelegramBot.Features.CoinProducts.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.CoinProducts.Screens;

public sealed class CreateCoinProductPriceScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        return ValueTask.FromResult(new ScreenView("<b>Новый товар</b>\n\nВведите цену в монетках:")
            .AwaitInput<EnterCreateCoinProductPriceAction>()
            .BackButton());
    }
}
