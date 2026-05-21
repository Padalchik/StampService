using StampService.TelegramBot.Features.Coins.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Coins.Screens;

public sealed class CoinCustomerPhoneScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        return ValueTask.FromResult(new ScreenView(
            "<b>Начислить монетки</b>\n\n" +
            "Введите телефон клиента в международном формате, например +7 999 123-45-67:")
            .AwaitInput<EnterCoinCustomerPhoneAction>()
            .BackButton());
    }
}
