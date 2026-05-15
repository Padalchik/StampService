using TelegramBotFlow.Core.Endpoints;

namespace StampService.TelegramBot.Features.Admin.Actions;

public sealed class StartEditRewardDigestSettingAction : IBotAction;

public record StartEditRewardDigestSettingPayload(
    string SettingKey,
    string Label);
