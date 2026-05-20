using StampService.TelegramBot.Features.IssueMetric.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.IssueMetric.Screens;

public sealed class IssueMetricCommentScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var metricName = ctx.Session?.Data.GetString(IssueMetricSessionKeys.MetricName) ?? "метрика";
        var recipientPhoneNumber = ctx.Session?.Data.GetString(IssueMetricSessionKeys.RecipientPhoneNumber);
        var amount = ctx.Session?.Data.Get<int>(IssueMetricSessionKeys.Amount) ?? 0;

        return ValueTask.FromResult(new ScreenView(
            "<b>Выдать метрику</b>\n\n" +
            $"Метрика: {metricName}\n" +
            $"Телефон клиента: {recipientPhoneNumber}\n" +
            $"Количество: {amount}\n\n" +
            "Введите комментарий.")
            .AwaitInput<EnterIssueCommentAction>()
            .BackButton());
    }
}
