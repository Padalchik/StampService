using System.Net;
using StampService.TelegramBot.Features.Metrics.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Metrics.Screens;

public sealed class EditMetricConfirmScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var name = ctx.Session?.Data.GetString(MetricManagementSessionKeys.EditName) ?? "-";
        var redemptionAmount = ctx.Session?.Data.Get<int>(MetricManagementSessionKeys.EditRedemptionAmount) ?? 0;

        return ValueTask.FromResult(new ScreenView(
            "<b>Сохранить изменения?</b>\n\n" +
            $"Название: {Html(name)}\n" +
            $"Списание: {redemptionAmount}")
            .Button<ConfirmEditMetricAction>("✅ Подтвердить")
            .Row()
            .Button<CancelEditMetricAction>("❌ Отмена"));
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
