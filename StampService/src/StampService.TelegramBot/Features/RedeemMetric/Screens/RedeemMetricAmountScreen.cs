using StampService.TelegramBot.Features.RedeemMetric.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.RedeemMetric.Screens;

public sealed class RedeemMetricAmountScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var metricName = ctx.Session?.Data.GetString(RedeemMetricSessionKeys.MetricName) ?? "метрика";
        var redemptionCode = ctx.Session?.Data.GetString(RedeemMetricSessionKeys.RedemptionCode);

        return ValueTask.FromResult(new ScreenView(
            "<b>Списание метрики</b>\n\n" +
            $"Метрика: {metricName}\n" +
            $"Код списания: {redemptionCode}\n\n" +
            "Введите количество для списания:")
            .AwaitInput<EnterRedeemAmountAction>()
            .BackButton());
    }
}
