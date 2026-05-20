using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;
using StampService.TelegramBot.Features.IssueMetric.Actions;

namespace StampService.TelegramBot.Features.IssueMetric.Screens;

public sealed class IssueMetricRecipientScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var metricName = ctx.Session?.Data.GetString(IssueMetricSessionKeys.MetricName) ?? "метрика";

        return ValueTask.FromResult(new ScreenView(
            "<b>Выдать метрику</b>\n\n" +
            $"Метрика: {metricName}\n\n" +
            "Введите телефон клиента в международном формате, например +7 999 123-45-67.")
            .AwaitInput<EnterIssueRecipientAction>()
            .BackButton());
    }
}
