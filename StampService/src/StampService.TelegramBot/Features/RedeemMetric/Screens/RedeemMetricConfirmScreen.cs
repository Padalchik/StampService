using System.Net;
using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.RedeemMetric.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.RedeemMetric.Screens;

public sealed class RedeemMetricConfirmScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var brandName = ctx.Session?.Data.GetString(BrandWorkspaceScreen.BrandNameSessionKey) ?? "бренд";
        var metricName = ctx.Session?.Data.GetString(RedeemMetricSessionKeys.MetricName) ?? "метрика";
        var customerName = ctx.Session?.Data.GetString(RedeemMetricSessionKeys.CustomerName) ?? "клиент";
        var redemptionCode = ctx.Session?.Data.GetString(RedeemMetricSessionKeys.RedemptionCode) ?? "-";
        var redemptionAmount = ctx.Session?.Data.Get<int>(RedeemMetricSessionKeys.RedemptionAmount) ?? 0;
        var currentBalance = ctx.Session?.Data.Get<int>(RedeemMetricSessionKeys.CurrentBalance) ?? 0;
        return ValueTask.FromResult(new ScreenView(
            "<b>Подтвердите списание</b>\n\n" +
            $"Бренд: {Html(brandName)}\n" +
            $"Клиент: {Html(customerName)}\n" +
            $"Метрика: {Html(metricName)}\n" +
            $"Код списания клиента: <code>{Html(redemptionCode)}</code>\n" +
            $"Баланс: {currentBalance}/{redemptionAmount}")
            .Button<ConfirmRedeemMetricAction>("✅ Подтвердить")
            .Row()
            .Button<CancelRedeemMetricAction>("❌ Отмена"));
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
