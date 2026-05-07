using StampService.TelegramBot.Features.RedeemMetric.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.RedeemMetric.Screens;

public sealed class RedeemMetricCodeScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var metricName = ctx.Session?.Data.GetString(RedeemMetricSessionKeys.MetricName) ?? "метрика";

        return ValueTask.FromResult(new ScreenView(
            "<b>Списание метрики</b>\n\n" +
            $"Метрика: {metricName}\n\n" +
            "Введите одноразовый код клиента для списания:")
            .AwaitInput<EnterRedeemCodeAction>()
            .BackButton());
    }
}
