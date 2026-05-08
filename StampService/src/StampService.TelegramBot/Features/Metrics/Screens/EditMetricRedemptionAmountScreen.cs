using StampService.TelegramBot.Features.Metrics.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Metrics.Screens;

public sealed class EditMetricRedemptionAmountScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var current = ctx.Session?.Data.Get<int>(MetricManagementSessionKeys.SelectedMetricRedemptionAmount) ?? 0;
        return ValueTask.FromResult(new ScreenView(
            "<b>Редактирование метрики</b>\n\n" +
            $"Текущее списание: {current}\n" +
            "Введите новое количество или '-' чтобы оставить текущее:")
            .AwaitInput<EnterEditMetricRedemptionAmountAction>()
            .BackButton());
    }
}
