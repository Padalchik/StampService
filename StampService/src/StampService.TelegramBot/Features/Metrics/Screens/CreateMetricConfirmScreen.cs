using System.Net;
using StampService.TelegramBot.Features.Metrics.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Metrics.Screens;

public sealed class CreateMetricConfirmScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var name = ctx.Session?.Data.GetString(MetricManagementSessionKeys.CreateName) ?? "-";
        var code = ctx.Session?.Data.GetString(MetricManagementSessionKeys.CreateCode) ?? "-";
        var redemptionAmount = ctx.Session?.Data.Get<int>(MetricManagementSessionKeys.CreateRedemptionAmount) ?? 0;

        return ValueTask.FromResult(new ScreenView(
            "<b>Создать метрику?</b>\n\n" +
            $"Название: {Html(name)}\n" +
            $"Код: <code>{Html(code)}</code>\n" +
            $"Списание: {redemptionAmount}")
            .Button<ConfirmCreateMetricAction>("✅ Подтвердить")
            .Row()
            .Button<CancelCreateMetricAction>("❌ Отмена"));
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
