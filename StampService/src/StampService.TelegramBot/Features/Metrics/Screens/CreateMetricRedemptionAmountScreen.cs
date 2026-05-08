using StampService.TelegramBot.Features.Metrics.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Metrics.Screens;

public sealed class CreateMetricRedemptionAmountScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        return ValueTask.FromResult(new ScreenView("<b>Новая метрика</b>\n\nСколько единиц списывать за одно списание?")
            .AwaitInput<EnterCreateMetricRedemptionAmountAction>()
            .BackButton());
    }
}
