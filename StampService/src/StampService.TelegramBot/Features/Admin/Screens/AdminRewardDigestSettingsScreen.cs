using StampService.Application.Abstractions;
using StampService.Application.Administration;
using StampService.Application.CustomerNotifications.Queries.GetRewardDigestSettings;
using StampService.Contracts.DTOs.CustomerNotifications;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Features.Admin.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Admin.Screens;

public sealed class AdminRewardDigestSettingsScreen : IScreen
{
    private readonly IAdminAccessService _adminAccessService;
    private readonly IQueryHandler<RewardDigestSettingsResponse, GetRewardDigestSettingsQuery> _settingsHandler;

    public AdminRewardDigestSettingsScreen(
        IAdminAccessService adminAccessService,
        IQueryHandler<RewardDigestSettingsResponse, GetRewardDigestSettingsQuery> settingsHandler)
    {
        _adminAccessService = adminAccessService;
        _settingsHandler = settingsHandler;
    }

    public async ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        if (!_adminAccessService.IsAdmin(ctx.UserId))
            return new ScreenView("Нет доступа к админке.").BackButton();

        var result = await _settingsHandler.Handle(
            new GetRewardDigestSettingsQuery(ctx.UserId),
            ctx.CancellationToken);
        if (result.IsFailed)
            return new ScreenView($"Не удалось загрузить настройки: {BotErrorFormatter.Format(result.Errors)}").BackButton();

        var settings = result.Value;
        var view = new ScreenView(
            "<b>Дайджест доступных наград</b>\n\n" +
            $"Включён: {FormatEnabled(settings.Enabled)}\n" +
            $"Интервал сообщений пользователю: {settings.MessageToUserIntervalMinutes} мин.\n" +
            $"Интервал проверки: {settings.ScanIntervalMinutes} мин.\n" +
            $"Размер пачки пользователей: {settings.BatchSize}\n" +
            $"Брендов в сообщении: {settings.MaxBrandsPerMessage}\n" +
            $"Наград на бренд: {settings.MaxRewardsPerBrand}");

        view.Button<ToggleRewardDigestEnabledAction>(settings.Enabled ? "Выключить" : "Включить");
        view.Row().Button<StartEditRewardDigestSettingAction, StartEditRewardDigestSettingPayload>(
            "Интервал сообщений",
            new StartEditRewardDigestSettingPayload("message_interval", "Интервал сообщений пользователю, минут"));
        view.Row().Button<StartEditRewardDigestSettingAction, StartEditRewardDigestSettingPayload>(
            "Интервал проверки",
            new StartEditRewardDigestSettingPayload("scan_interval", "Интервал проверки, минут"));
        view.Row().Button<StartEditRewardDigestSettingAction, StartEditRewardDigestSettingPayload>(
            "Размер пачки",
            new StartEditRewardDigestSettingPayload("batch_size", "Размер пачки пользователей"));
        view.Row().Button<StartEditRewardDigestSettingAction, StartEditRewardDigestSettingPayload>(
            "Брендов в сообщении",
            new StartEditRewardDigestSettingPayload("max_brands", "Максимум брендов в сообщении"));
        view.Row().Button<StartEditRewardDigestSettingAction, StartEditRewardDigestSettingPayload>(
            "Наград на бренд",
            new StartEditRewardDigestSettingPayload("max_rewards", "Максимум наград на бренд"));

        return view.Row()
            .NavigateButton<AdminPanelScreen>("К админке")
            .BackButton();
    }

    private static string FormatEnabled(bool value) => value ? "да" : "нет";
}
