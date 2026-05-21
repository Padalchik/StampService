using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;
using StampService.TelegramBot.Features.IssueMetric.Actions;

namespace StampService.TelegramBot.Features.IssueMetric.Screens;

public sealed class IssueMetricAmountScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var metricName = ctx.Session?.Data.GetString(IssueMetricSessionKeys.MetricName) ?? "метрика";
        var recipientPhoneNumber = ctx.Session?.Data.GetString(IssueMetricSessionKeys.RecipientPhoneNumber);

        return ValueTask.FromResult(new ScreenView(
            "<b>Выдать метрику</b>\n\n" +
            $"Метрика: {metricName}\n" +
            $"Телефон клиента: {recipientPhoneNumber}\n\n" +
            "Введите количество.")
            .AwaitInput<EnterIssueAmountAction>()
            .BackButton());
    }
}
