using StampService.TelegramBot.Features.Admin.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Admin.Screens;

public sealed class CreateBrandNameScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        return ValueTask.FromResult(new ScreenView("<b>Новый бренд</b>\n\nВведите название бренда:")
            .AwaitInput<EnterCreateBrandNameAction>()
            .BackButton());
    }
}
