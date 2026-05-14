using StampService.Application.Abstractions;
using StampService.Application.Brands.Commands.UpdateBrandRewardSettings;
using StampService.Application.Brands.Queries.GetBrandWorkspace;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Brands;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Features.Brands.Actions;
using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.CoinProducts.Screens;
using StampService.TelegramBot.Features.Metrics.Screens;
using StampService.TelegramBot.Features.Staff.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Brands.Endpoints;

public sealed class BrandWorkspaceEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapAction<OpenBrandSettingsAction>(OpenSettingsAsync);
        app.MapAction<ToggleBrandSettingsMetricsAction>(ToggleMetricsAsync);
        app.MapAction<ToggleBrandSettingsCoinsAction>(ToggleCoinsAsync);
        app.MapAction<ToggleBrandSettingsCoinProductRedemptionAction>(ToggleCoinProductRedemptionAsync);
        app.MapAction<ToggleBrandSettingsManualCoinRedemptionAction>(ToggleManualCoinRedemptionAsync);
        app.MapAction<ConfirmBrandSettingsAction>(ConfirmSettingsAsync);
        app.MapAction<CancelBrandSettingsAction>(CancelSettingsAsync);

        app.MapAction<OpenBrandWorkspaceAction, OpenBrandWorkspacePayload>(async (
            UpdateContext ctx,
            OpenBrandWorkspacePayload payload,
            ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
            IQueryHandler<BrandWorkspaceResponse, GetBrandWorkspaceQuery> workspaceHandler) =>
        {
            ctx.Session?.Data.Set(BrandWorkspaceScreen.BrandIdSessionKey, payload.BrandId);

            var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
            var userResult = await ensureUserHandler.Handle(
                new EnsureTelegramUserCommand(
                    ctx.UserId,
                    from?.FirstName,
                    from?.LastName,
                    from?.Username),
                ctx.CancellationToken);

            if (userResult.IsFailed)
                return BotResults.NavigateTo<BrandWorkspaceScreen>();

            var workspaceResult = await workspaceHandler.Handle(
                new GetBrandWorkspaceQuery(userResult.Value.UserId, payload.BrandId),
                ctx.CancellationToken);

            if (workspaceResult.IsFailed)
                return BotResults.NavigateTo<BrandWorkspaceScreen>();

            var workspace = workspaceResult.Value;
            StoreWorkspace(ctx, workspace);

            var directSection = GetSingleAvailableSection(workspace);
            return directSection?.Invoke() ?? BotResults.NavigateTo<BrandWorkspaceScreen>();
        });
    }

    private static Func<IEndpointResult>? GetSingleAvailableSection(BrandWorkspaceResponse workspace)
    {
        var sections = new List<Func<IEndpointResult>>();

        if (workspace.CanIssue || workspace.CanRedeem || workspace.CanViewBalances)
            sections.Add(() => BotResults.NavigateTo<ClientWorkScreen>());

        if (workspace.CanManageMetrics && workspace.IsMetricsEnabled)
            sections.Add(() => BotResults.NavigateTo<MetricsListScreen>());

        if (workspace.CanManageMetrics && workspace.IsCoinsEnabled && workspace.IsCoinProductRedemptionEnabled)
            sections.Add(() => BotResults.NavigateTo<CoinProductsListScreen>());

        if (workspace.CanManageStaff)
            sections.Add(() => BotResults.NavigateTo<BrandStaffListScreen>());

        if (workspace.CanManageBrand)
            sections.Add(() => BotResults.NavigateTo<BrandSettingsScreen>());

        return sections.Count == 1 ? sections[0] : null;
    }

    private static void StoreWorkspace(UpdateContext ctx, BrandWorkspaceResponse workspace)
    {
        ctx.Session?.Data.Set(BrandWorkspaceScreen.BrandNameSessionKey, workspace.BrandName);
        ctx.Session?.Data.Set(BrandWorkspaceScreen.CanIssueSessionKey, workspace.CanIssue);
        ctx.Session?.Data.Set(BrandWorkspaceScreen.CanRedeemSessionKey, workspace.CanRedeem);
        ctx.Session?.Data.Set(BrandWorkspaceScreen.CanViewBalancesSessionKey, workspace.CanViewBalances);
        ctx.Session?.Data.Set(BrandWorkspaceScreen.CanManageBrandSessionKey, workspace.CanManageBrand);
        ctx.Session?.Data.Set(BrandWorkspaceScreen.CanManageMetricsSessionKey, workspace.CanManageMetrics);
        ctx.Session?.Data.Set(BrandWorkspaceScreen.CanManageStaffSessionKey, workspace.CanManageStaff);
        ctx.Session?.Data.Set(BrandWorkspaceScreen.IsMetricsEnabledSessionKey, workspace.IsMetricsEnabled);
        ctx.Session?.Data.Set(BrandWorkspaceScreen.IsCoinsEnabledSessionKey, workspace.IsCoinsEnabled);
        ctx.Session?.Data.Set(BrandWorkspaceScreen.IsCoinProductRedemptionEnabledSessionKey, workspace.IsCoinProductRedemptionEnabled);
        ctx.Session?.Data.Set(BrandWorkspaceScreen.IsManualCoinRedemptionEnabledSessionKey, workspace.IsManualCoinRedemptionEnabled);
    }

    private static Task<IEndpointResult> OpenSettingsAsync(UpdateContext ctx)
    {
        var canManageBrand = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.CanManageBrandSessionKey) ?? false;
        if (!canManageBrand)
            return Task.FromResult(BotResults.ShowView(new ScreenView("Нет доступа к настройкам бренда.").BackButton()));

        ctx.Session?.Data.Set(
            BrandWorkspaceScreen.EditMetricsEnabledSessionKey,
            ctx.Session.Data.GetBool(BrandWorkspaceScreen.IsMetricsEnabledSessionKey) ?? true);
        ctx.Session?.Data.Set(
            BrandWorkspaceScreen.EditCoinsEnabledSessionKey,
            ctx.Session.Data.GetBool(BrandWorkspaceScreen.IsCoinsEnabledSessionKey) ?? true);
        ctx.Session?.Data.Set(
            BrandWorkspaceScreen.EditCoinProductRedemptionEnabledSessionKey,
            ctx.Session.Data.GetBool(BrandWorkspaceScreen.IsCoinProductRedemptionEnabledSessionKey) ?? true);
        ctx.Session?.Data.Set(
            BrandWorkspaceScreen.EditManualCoinRedemptionEnabledSessionKey,
            ctx.Session.Data.GetBool(BrandWorkspaceScreen.IsManualCoinRedemptionEnabledSessionKey) ?? false);

        return Task.FromResult(BotResults.NavigateTo<BrandSettingsScreen>());
    }

    private static Task<IEndpointResult> ToggleMetricsAsync(UpdateContext ctx)
    {
        var current = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.EditMetricsEnabledSessionKey)
            ?? ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.IsMetricsEnabledSessionKey)
            ?? true;
        ctx.Session?.Data.Set(BrandWorkspaceScreen.EditMetricsEnabledSessionKey, !current);
        return Task.FromResult(BotResults.NavigateTo<BrandSettingsScreen>());
    }

    private static Task<IEndpointResult> ToggleCoinsAsync(UpdateContext ctx)
    {
        var current = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.EditCoinsEnabledSessionKey)
            ?? ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.IsCoinsEnabledSessionKey)
            ?? true;
        ctx.Session?.Data.Set(BrandWorkspaceScreen.EditCoinsEnabledSessionKey, !current);
        return Task.FromResult(BotResults.NavigateTo<BrandSettingsScreen>());
    }

    private static Task<IEndpointResult> ToggleCoinProductRedemptionAsync(UpdateContext ctx)
    {
        var current = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.EditCoinProductRedemptionEnabledSessionKey)
            ?? ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.IsCoinProductRedemptionEnabledSessionKey)
            ?? true;
        ctx.Session?.Data.Set(BrandWorkspaceScreen.EditCoinProductRedemptionEnabledSessionKey, !current);
        return Task.FromResult(BotResults.NavigateTo<BrandSettingsScreen>());
    }

    private static Task<IEndpointResult> ToggleManualCoinRedemptionAsync(UpdateContext ctx)
    {
        var current = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.EditManualCoinRedemptionEnabledSessionKey)
            ?? ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.IsManualCoinRedemptionEnabledSessionKey)
            ?? false;
        ctx.Session?.Data.Set(BrandWorkspaceScreen.EditManualCoinRedemptionEnabledSessionKey, !current);
        return Task.FromResult(BotResults.NavigateTo<BrandSettingsScreen>());
    }

    private static async Task<IEndpointResult> ConfirmSettingsAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        ICommandHandler<UpdateBrandResponse, UpdateBrandRewardSettingsCommand> updateHandler)
    {
        var brandId = ctx.Session?.Data.Get<Guid>(BrandWorkspaceScreen.BrandIdSessionKey) ?? Guid.Empty;
        var isMetricsEnabled = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.EditMetricsEnabledSessionKey) ?? true;
        var isCoinsEnabled = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.EditCoinsEnabledSessionKey) ?? true;
        var isCoinProductRedemptionEnabled = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.EditCoinProductRedemptionEnabledSessionKey) ?? true;
        var isManualCoinRedemptionEnabled = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.EditManualCoinRedemptionEnabledSessionKey) ?? false;

        if (brandId == Guid.Empty)
            return BotResults.ShowView(new ScreenView("Бренд не выбран.").BackButton());

        var from = ctx.Update.CallbackQuery?.From ?? ctx.Update.Message?.From;
        var userResult = await ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(
                ctx.UserId,
                from?.FirstName,
                from?.LastName,
                from?.Username),
            ctx.CancellationToken);

        if (userResult.IsFailed)
            return BotResults.ShowView(new ScreenView("Не удалось определить пользователя.").BackButton());

        var result = await updateHandler.Handle(
            new UpdateBrandRewardSettingsCommand(
                userResult.Value.UserId,
                brandId,
                isMetricsEnabled,
                isCoinsEnabled,
                isCoinProductRedemptionEnabled,
                isManualCoinRedemptionEnabled),
            ctx.CancellationToken);

        if (result.IsFailed)
            return BotResults.ShowView(new ScreenView($"Не удалось сохранить настройки: {BotErrorFormatter.Format(result.Errors)}").BackButton());

        ctx.Session?.Data.Set(BrandWorkspaceScreen.BrandNameSessionKey, result.Value.BrandName);
        ctx.Session?.Data.Set(BrandWorkspaceScreen.IsMetricsEnabledSessionKey, result.Value.IsMetricsEnabled);
        ctx.Session?.Data.Set(BrandWorkspaceScreen.IsCoinsEnabledSessionKey, result.Value.IsCoinsEnabled);
        ctx.Session?.Data.Set(BrandWorkspaceScreen.IsCoinProductRedemptionEnabledSessionKey, result.Value.IsCoinProductRedemptionEnabled);
        ctx.Session?.Data.Set(BrandWorkspaceScreen.IsManualCoinRedemptionEnabledSessionKey, result.Value.IsManualCoinRedemptionEnabled);
        ClearEditSettings(ctx);

        return BotResults.ShowView(new ScreenView(
            "<b>Настройки сохранены</b>\n\n" +
            $"{result.Value.BrandName}\n" +
            $"Метрики: {FormatEnabled(result.Value.IsMetricsEnabled)}\n" +
            $"Монетки: {FormatEnabled(result.Value.IsCoinsEnabled)}")
            .NavigateButton<BrandWorkspaceScreen>("К бренду"));
    }

    private static Task<IEndpointResult> CancelSettingsAsync(UpdateContext ctx)
    {
        ClearEditSettings(ctx);
        return Task.FromResult(BotResults.NavigateTo<BrandWorkspaceScreen>());
    }

    private static void ClearEditSettings(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(BrandWorkspaceScreen.EditMetricsEnabledSessionKey);
        ctx.Session?.Data.Remove(BrandWorkspaceScreen.EditCoinsEnabledSessionKey);
        ctx.Session?.Data.Remove(BrandWorkspaceScreen.EditCoinProductRedemptionEnabledSessionKey);
        ctx.Session?.Data.Remove(BrandWorkspaceScreen.EditManualCoinRedemptionEnabledSessionKey);
    }

    private static string FormatEnabled(bool value) => value ? "включены" : "выключены";
}
