using StampService.TelegramBot.Features.RedeemMetric.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.RedeemMetric.Screens;

public sealed class RedeemMetricCodeScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        return ValueTask.FromResult(new ScreenView(
            "<b>Списание метрики</b>\n\n" +
            "Введите код списания клиента:")
            .AwaitInput<EnterRedeemCodeAction>()
            .BackButton());
    }
}
