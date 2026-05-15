using StampService.Application.Abstractions;
using StampService.Application.CustomerNotifications.Commands.UpdateRewardDigestSettings;
using StampService.Application.CustomerNotifications.Queries.GetRewardDigestSettings;
using StampService.Contracts.DTOs.CustomerNotifications;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Common.Routing;
using StampService.TelegramBot.Features.Admin.Actions;
using StampService.TelegramBot.Features.Admin.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Admin.Endpoints;

public sealed class AdminRewardDigestEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapAction<ToggleRewardDigestEnabledAction>(ToggleEnabledAsync);
        app.MapAction<StartEditRewardDigestSettingAction, StartEditRewardDigestSettingPayload>(StartEditSettingAsync);
        app.MapInput<EnterRewardDigestSettingValueAction>(EnterSettingValueAsync);
    }

    private static async Task<IEndpointResult> ToggleEnabledAsync(
        UpdateContext ctx,
        IQueryHandler<RewardDigestSettingsResponse, GetRewardDigestSettingsQuery> getHandler,
        ICommandHandler<RewardDigestSettingsResponse, UpdateRewardDigestSettingsCommand> updateHandler)
    {
        var currentResult = await getHandler.Handle(
            new GetRewardDigestSettingsQuery(ctx.UserId),
            ctx.CancellationToken);
        if (currentResult.IsFailed)
            return Error(currentResult.Errors);

        var current = currentResult.Value;
        var updateResult = await updateHandler.Handle(
            new UpdateRewardDigestSettingsCommand(
                ctx.UserId,
                !current.Enabled,
                current.MessageToUserIntervalMinutes,
                current.ScanIntervalMinutes,
                current.BatchSize,
                current.MaxBrandsPerMessage,
                current.MaxRewardsPerBrand),
            ctx.CancellationToken);

        return updateResult.IsFailed
            ? Error(updateResult.Errors)
            : BotResults.NavigateTo<AdminRewardDigestSettingsScreen>();
    }

    private static Task<IEndpointResult> StartEditSettingAsync(
        UpdateContext ctx,
        StartEditRewardDigestSettingPayload payload)
    {
        ctx.Session?.Data.Set(AdminSessionKeys.RewardDigestEditSettingKey, payload.SettingKey);
        ctx.Session?.Data.Set(AdminSessionKeys.RewardDigestEditSettingLabel, payload.Label);

        return Task.FromResult(BotResults.NavigateTo<AdminRewardDigestEditValueScreen>());
    }

    private static async Task<IEndpointResult> EnterSettingValueAsync(
        UpdateContext ctx,
        IQueryHandler<RewardDigestSettingsResponse, GetRewardDigestSettingsQuery> getHandler,
        ICommandHandler<RewardDigestSettingsResponse, UpdateRewardDigestSettingsCommand> updateHandler)
    {
        var settingKey = ctx.Session?.Data.GetString(AdminSessionKeys.RewardDigestEditSettingKey) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(settingKey))
            return BotResults.ShowView(new ScreenView("Сценарий редактирования устарел. Откройте настройки заново.").BackButton());

        if (!int.TryParse(ctx.MessageText?.Trim(), out var value) || value <= 0)
        {
            return BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView("Введите положительное целое число.")
                .AwaitInput<EnterRewardDigestSettingValueAction>()
                .BackButton()));
        }

        var currentResult = await getHandler.Handle(
            new GetRewardDigestSettingsQuery(ctx.UserId),
            ctx.CancellationToken);
        if (currentResult.IsFailed)
            return Error(currentResult.Errors);

        var current = currentResult.Value;
        var updateResult = await updateHandler.Handle(
            BuildUpdateCommand(ctx.UserId, current, settingKey, value),
            ctx.CancellationToken);

        if (updateResult.IsFailed)
            return Error(updateResult.Errors);

        ctx.Session?.Data.Remove(AdminSessionKeys.RewardDigestEditSettingKey);
        ctx.Session?.Data.Remove(AdminSessionKeys.RewardDigestEditSettingLabel);

        return BotInputResults.DeleteInputThen(BotResults.NavigateTo<AdminRewardDigestSettingsScreen>());
    }

    private static UpdateRewardDigestSettingsCommand BuildUpdateCommand(
        long adminTelegramUserId,
        RewardDigestSettingsResponse current,
        string settingKey,
        int value)
    {
        return settingKey switch
        {
            "message_interval" => ToCommand(current with { MessageToUserIntervalMinutes = value }, adminTelegramUserId),
            "scan_interval" => ToCommand(current with { ScanIntervalMinutes = value }, adminTelegramUserId),
            "batch_size" => ToCommand(current with { BatchSize = value }, adminTelegramUserId),
            "max_brands" => ToCommand(current with { MaxBrandsPerMessage = value }, adminTelegramUserId),
            "max_rewards" => ToCommand(current with { MaxRewardsPerBrand = value }, adminTelegramUserId),
            _ => ToCommand(current, adminTelegramUserId)
        };
    }

    private static UpdateRewardDigestSettingsCommand ToCommand(
        RewardDigestSettingsResponse settings,
        long adminTelegramUserId)
    {
        return new UpdateRewardDigestSettingsCommand(
            adminTelegramUserId,
            settings.Enabled,
            settings.MessageToUserIntervalMinutes,
            settings.ScanIntervalMinutes,
            settings.BatchSize,
            settings.MaxBrandsPerMessage,
            settings.MaxRewardsPerBrand);
    }

    private static IEndpointResult Error(IReadOnlyList<FluentResults.IError> errors)
    {
        return BotResults.ShowView(new ScreenView($"Не удалось обновить настройки: {BotErrorFormatter.Format(errors)}").BackButton());
    }
}
