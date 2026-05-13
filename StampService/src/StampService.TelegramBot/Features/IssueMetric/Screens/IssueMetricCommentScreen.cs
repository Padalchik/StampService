using StampService.TelegramBot.Features.IssueMetric.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.IssueMetric.Screens;

public sealed class IssueMetricCommentScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var metricName = ctx.Session?.Data.GetString(IssueMetricSessionKeys.MetricName) ?? "метрика";
        var recipientCustomerCode = ctx.Session?.Data.GetString(IssueMetricSessionKeys.RecipientCustomerCode);
        var amount = ctx.Session?.Data.Get<int>(IssueMetricSessionKeys.Amount) ?? 0;

        return ValueTask.FromResult(new ScreenView(
            "<b>Выдать метрику</b>\n\n" +
            $"Метрика: {metricName}\n" +
            $"Код пользователя: {recipientCustomerCode}\n" +
            $"Количество: {amount}\n\n" +
            "Введите комментарий.")
            .AwaitInput<EnterIssueCommentAction>()
            .BackButton());
    }
}
