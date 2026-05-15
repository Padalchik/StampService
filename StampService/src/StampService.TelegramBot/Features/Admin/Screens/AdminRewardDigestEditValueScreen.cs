using StampService.TelegramBot.Features.Admin.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Admin.Screens;

public sealed class AdminRewardDigestEditValueScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var label = ctx.Session?.Data.GetString(AdminSessionKeys.RewardDigestEditSettingLabel) ?? "значение";

        return ValueTask.FromResult(new ScreenView(
            $"<b>Дайджест наград</b>\n\nВведите новое значение для настройки:\n{label}")
            .AwaitInput<EnterRewardDigestSettingValueAction>()
            .BackButton());
    }
}
