using StampService.TelegramBot.Features.Metrics.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Metrics.Screens;

public sealed class CreateMetricCodeScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        return ValueTask.FromResult(new ScreenView("<b>Новая метрика</b>\n\nВведите короткий код:")
            .AwaitInput<EnterCreateMetricCodeAction>()
            .BackButton());
    }
}
