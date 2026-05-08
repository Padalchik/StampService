using StampService.TelegramBot.Features.Coins.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Coins.Screens;

public sealed class CoinRedemptionCodeScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        return ValueTask.FromResult(new ScreenView(
            "<b>Списать монетки</b>\n\n" +
            "Введите одноразовый код клиента:")
            .AwaitInput<EnterCoinRedemptionCodeAction>()
            .BackButton());
    }
}
