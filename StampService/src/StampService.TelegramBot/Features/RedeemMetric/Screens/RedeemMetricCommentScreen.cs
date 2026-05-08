using StampService.TelegramBot.Features.RedeemMetric.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.RedeemMetric.Screens;

public sealed class RedeemMetricCommentScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var metricName = ctx.Session?.Data.GetString(RedeemMetricSessionKeys.MetricName) ?? "метрика";
        var redemptionCode = ctx.Session?.Data.GetString(RedeemMetricSessionKeys.RedemptionCode);
        var redemptionAmount = ctx.Session?.Data.Get<int>(RedeemMetricSessionKeys.RedemptionAmount) ?? 0;

        return ValueTask.FromResult(new ScreenView(
            "<b>Списание метрики</b>\n\n" +
            $"Метрика: {metricName}\n" +
            $"Код списания: {redemptionCode}\n" +
            $"Количество: {redemptionAmount}\n\n" +
            "Введите комментарий:")
            .AwaitInput<EnterRedeemCommentAction>()
            .BackButton());
    }
}
