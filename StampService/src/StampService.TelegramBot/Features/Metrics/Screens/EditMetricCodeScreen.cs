using System.Net;
using StampService.TelegramBot.Features.Metrics.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Metrics.Screens;

public sealed class EditMetricCodeScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var current = ctx.Session?.Data.GetString(MetricManagementSessionKeys.SelectedMetricCode) ?? "-";
        return ValueTask.FromResult(new ScreenView(
            "<b>Редактирование метрики</b>\n\n" +
            $"Текущий код: <code>{Html(current)}</code>\n" +
            "Введите новый код:")
            .AwaitInput<EnterEditMetricCodeAction>()
            .Button<KeepEditMetricCodeAction>("Оставить эти данные")
            .BackButton());
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
