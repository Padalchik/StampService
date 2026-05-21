using System.Net;
using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.IssueMetric.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.IssueMetric.Screens;

public sealed class IssueMetricConfirmScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var brandName = ctx.Session?.Data.GetString(BrandWorkspaceScreen.BrandNameSessionKey) ?? "бренд";
        var metricName = ctx.Session?.Data.GetString(IssueMetricSessionKeys.MetricName) ?? "метрика";
        var recipientPhoneNumber = ctx.Session?.Data.GetString(IssueMetricSessionKeys.RecipientPhoneNumber) ?? "-";
        var amount = ctx.Session?.Data.Get<int>(IssueMetricSessionKeys.Amount) ?? 0;
        return ValueTask.FromResult(new ScreenView(
            "<b>Подтвердите выдачу</b>\n\n" +
            $"Бренд: {Html(brandName)}\n" +
            $"Метрика: {Html(metricName)}\n" +
            $"Телефон клиента: <code>{Html(recipientPhoneNumber)}</code>\n" +
            $"Количество: {amount}")
            .Button<ConfirmIssueMetricAction>("✅ Подтвердить")
            .Row()
            .Button<CancelIssueMetricAction>("❌ Отмена"));
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
