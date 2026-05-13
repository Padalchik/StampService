using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;
using StampService.TelegramBot.Features.IssueMetric.Actions;

namespace StampService.TelegramBot.Features.IssueMetric.Screens;

public sealed class IssueMetricAmountScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var metricName = ctx.Session?.Data.GetString(IssueMetricSessionKeys.MetricName) ?? "метрика";
        var recipientCustomerCode = ctx.Session?.Data.GetString(IssueMetricSessionKeys.RecipientCustomerCode);

        return ValueTask.FromResult(new ScreenView(
            "<b>Выдать метрику</b>\n\n" +
            $"Метрика: {metricName}\n" +
            $"Код пользователя: {recipientCustomerCode}\n\n" +
            "Введите количество.")
            .AwaitInput<EnterIssueAmountAction>()
            .BackButton());
    }
}
