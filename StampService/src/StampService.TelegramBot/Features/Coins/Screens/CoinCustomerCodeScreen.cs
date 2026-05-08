using StampService.TelegramBot.Features.Coins.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Coins.Screens;

public sealed class CoinCustomerCodeScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var mode = ctx.Session?.Data.GetString(CoinSessionKeys.Mode);
        var title = mode == CoinSessionKeys.ModeIssue
            ? "Начислить монетки"
            : "Баланс монеток";

        return ValueTask.FromResult(new ScreenView(
            $"<b>{title}</b>\n\n" +
            "Введите CustomerCode клиента:")
            .AwaitInput<EnterCoinCustomerCodeAction>()
            .BackButton());
    }
}
