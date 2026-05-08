using System.Net;
using StampService.TelegramBot.Features.Coins.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Coins.Screens;

public sealed class CoinConfirmScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var mode = ctx.Session?.Data.GetString(CoinSessionKeys.Mode);
        var amount = ctx.Session?.Data.Get<int>(CoinSessionKeys.Amount) ?? 0;
        var comment = ctx.Session?.Data.GetString(CoinSessionKeys.Comment) ?? string.Empty;
        var customerCode = ctx.Session?.Data.GetString(CoinSessionKeys.CustomerCode);
        var redemptionCode = ctx.Session?.Data.GetString(CoinSessionKeys.RedemptionCode);

        var title = mode == CoinSessionKeys.ModeIssue
            ? "Подтвердите начисление"
            : "Подтвердите списание";

        var subject = mode == CoinSessionKeys.ModeIssue
            ? $"CustomerCode: <code>{Html(customerCode ?? "-")}</code>"
            : $"Код списания: <code>{Html(redemptionCode ?? "-")}</code>";

        var view = new ScreenView(
            $"<b>{title}</b>\n\n" +
            $"{subject}\n" +
            $"Количество: {amount}\n" +
            $"Комментарий: {Html(comment)}");

        if (mode == CoinSessionKeys.ModeIssue)
            view.Button<ConfirmIssueCoinsAction>("Подтвердить");
        else
            view.Button<ConfirmRedeemCoinsAction>("Подтвердить");

        return ValueTask.FromResult(view.Row()
            .Button<CancelCoinOperationAction>("Отмена"));
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
