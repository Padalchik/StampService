using System.Net;
using StampService.TelegramBot.Features.RedeemMetric.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.RedeemMetric.Screens;

public sealed class RedeemMetricCommentScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var metricName = ctx.Session?.Data.GetString(RedeemMetricSessionKeys.MetricName) ?? "метрика";
        var customerName = ctx.Session?.Data.GetString(RedeemMetricSessionKeys.CustomerName) ?? "клиент";
        var redemptionCode = ctx.Session?.Data.GetString(RedeemMetricSessionKeys.RedemptionCode);
        var redemptionAmount = ctx.Session?.Data.Get<int>(RedeemMetricSessionKeys.RedemptionAmount) ?? 0;
        var currentBalance = ctx.Session?.Data.Get<int>(RedeemMetricSessionKeys.CurrentBalance) ?? 0;

        return ValueTask.FromResult(new ScreenView(
            "<b>Списание метрики</b>\n\n" +
            $"Клиент: {Html(customerName)}\n" +
            $"Метрика: {Html(metricName)}\n" +
            $"Код списания клиента: <code>{Html(redemptionCode ?? string.Empty)}</code>\n" +
            $"Баланс: {currentBalance}/{redemptionAmount}\n\n" +
            "Введите комментарий:")
            .AwaitInput<EnterRedeemCommentAction>()
            .BackButton());
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
