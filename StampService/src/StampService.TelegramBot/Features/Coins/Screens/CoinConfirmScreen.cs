using System.Net;
using StampService.TelegramBot.Features.Coins.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Coins.Screens;

public sealed class CoinConfirmScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var amount = ctx.Session?.Data.Get<int>(CoinSessionKeys.Amount) ?? 0;
        var customerCode = ctx.Session?.Data.GetString(CoinSessionKeys.CustomerCode);

        var view = new ScreenView(
            "<b>Подтвердите начисление</b>\n\n" +
            $"Код пользователя: <code>{Html(customerCode ?? "-")}</code>\n" +
            $"Количество: {amount}");

        view.Button<ConfirmIssueCoinsAction>("✅ Подтвердить");

        return ValueTask.FromResult(view.Row()
            .Button<CancelCoinOperationAction>("❌ Отмена"));
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
