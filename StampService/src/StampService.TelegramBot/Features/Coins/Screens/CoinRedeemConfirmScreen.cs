using System.Net;
using StampService.TelegramBot.Features.Coins.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Coins.Screens;

public sealed class CoinRedeemConfirmScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var code = ctx.Session?.Data.GetString(CoinSessionKeys.RedemptionCode) ?? string.Empty;
        var amount = ctx.Session?.Data.Get<int>(CoinSessionKeys.Amount) ?? 0;
        var comment = ctx.Session?.Data.GetString(CoinSessionKeys.Comment) ?? string.Empty;

        return ValueTask.FromResult(new ScreenView(
            "<b>Списать монетки?</b>\n\n" +
            $"Код списания: <code>{Html(code)}</code>\n" +
            $"Количество: {amount}\n" +
            $"Назначение: {Html(comment)}")
            .Button<ConfirmRedeemCoinsAction>("✅ Подтвердить")
            .Row()
            .Button<CancelCoinOperationAction>("❌ Отмена"));
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
