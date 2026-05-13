using StampService.TelegramBot.Features.Coins.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Coins.Screens;

public sealed class CoinCustomerCodeScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        return ValueTask.FromResult(new ScreenView(
            "<b>Начислить монетки</b>\n\n" +
            "Введите код пользователя клиента:")
            .AwaitInput<EnterCoinCustomerCodeAction>()
            .BackButton());
    }
}
