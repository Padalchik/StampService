using System.Net;
using StampService.TelegramBot.Features.Metrics.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Metrics.Screens;

public sealed class EditMetricNameScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var current = ctx.Session?.Data.GetString(MetricManagementSessionKeys.SelectedMetricName) ?? "-";
        return ValueTask.FromResult(new ScreenView(
            "<b>Редактирование метрики</b>\n\n" +
            $"Текущее название: {Html(current)}\n" +
            "Введите новое название:")
            .AwaitInput<EnterEditMetricNameAction>()
            .Button<KeepEditMetricNameAction>("Оставить эти данные")
            .BackButton());
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
