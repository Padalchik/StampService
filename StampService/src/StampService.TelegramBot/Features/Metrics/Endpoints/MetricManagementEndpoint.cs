using StampService.Application.Abstractions;
using StampService.Application.Metrics.Commands.CreateMetric;
using StampService.Application.Metrics.Commands.UpdateMetric;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Metrics;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Common.Routing;
using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.Metrics.Actions;
using StampService.TelegramBot.Features.Metrics.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Metrics.Endpoints;

public sealed class MetricManagementEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapAction<OpenMetricDetailsAction, OpenMetricDetailsPayload>(OpenDetailsAsync);
        app.MapAction<StartCreateMetricAction>(StartCreateAsync);
        app.MapInput<EnterCreateMetricNameAction>(EnterCreateNameAsync);
        app.MapInput<EnterCreateMetricRedemptionAmountAction>(EnterCreateRedemptionAmountAsync);
        app.MapAction<ConfirmCreateMetricAction>(ConfirmCreateAsync);
        app.MapAction<CancelCreateMetricAction>(CancelCreateAsync);
        app.MapAction<StartEditMetricAction>(StartEditAsync);
        app.MapAction<KeepEditMetricNameAction>(KeepEditNameAsync);
        app.MapAction<KeepEditMetricRedemptionAmountAction>(KeepEditRedemptionAmountAsync);
        app.MapInput<EnterEditMetricNameAction>(EnterEditNameAsync);
        app.MapInput<EnterEditMetricRedemptionAmountAction>(EnterEditRedemptionAmountAsync);
        app.MapAction<ConfirmEditMetricAction>(ConfirmEditAsync);
        app.MapAction<CancelEditMetricAction>(CancelEditAsync);
    }

    private static Task<IEndpointResult> OpenDetailsAsync(
        UpdateContext ctx,
        OpenMetricDetailsPayload payload)
    {
        ctx.Session?.Data.Set(MetricManagementSessionKeys.SelectedMetricDefinitionId, payload.MetricDefinitionId);
        return Task.FromResult(BotResults.NavigateTo<MetricDetailsScreen>());
    }

    private static Task<IEndpointResult> StartCreateAsync(UpdateContext ctx)
    {
        ClearCreateSession(ctx);
        return Task.FromResult(BotResults.NavigateTo<CreateMetricNameScreen>());
    }

    private static Task<IEndpointResult> EnterCreateNameAsync(UpdateContext ctx)
    {
        var name = ctx.MessageText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            return Retry<CreateMetricNameScreen, EnterCreateMetricNameAction>("Название обязательно.");

        ctx.Session?.Data.Set(MetricManagementSessionKeys.CreateName, name);
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.NavigateTo<CreateMetricRedemptionAmountScreen>()));
    }

    private static Task<IEndpointResult> EnterCreateRedemptionAmountAsync(UpdateContext ctx)
    {
        if (!int.TryParse(ctx.MessageText, out var amount) || amount <= 0)
            return Retry<CreateMetricRedemptionAmountScreen, EnterCreateMetricRedemptionAmountAction>("Количество для списания должно быть положительным числом.");

        ctx.Session?.Data.Set(MetricManagementSessionKeys.CreateRedemptionAmount, amount);
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.NavigateTo<CreateMetricConfirmScreen>()));
    }

    private static async Task<IEndpointResult> ConfirmCreateAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        ICommandHandler<MetricResponse, CreateMetricCommand> createMetricHandler)
    {
        var brandId = ctx.Session?.Data.Get<Guid>(BrandWorkspaceScreen.BrandIdSessionKey) ?? Guid.Empty;
        var name = ctx.Session?.Data.GetString(MetricManagementSessionKeys.CreateName) ?? string.Empty;
        var redemptionAmount = ctx.Session?.Data.Get<int>(MetricManagementSessionKeys.CreateRedemptionAmount) ?? 0;

        if (brandId == Guid.Empty || string.IsNullOrWhiteSpace(name) || redemptionAmount <= 0)
            return BotResults.ShowView(new ScreenView("Сценарий создания устарел. Начните заново.").BackButton());

        var userResult = await EnsureUserAsync(ctx, ensureUserHandler);
        if (userResult.IsFailed)
            return BotResults.ShowView(new ScreenView($"Не удалось определить пользователя: {BotErrorFormatter.Format(userResult.Errors)}").BackButton());

        var result = await createMetricHandler.Handle(
            new CreateMetricCommand(
                brandId,
                userResult.Value.UserId,
                new CreateMetricRequest(name, redemptionAmount)),
            ctx.CancellationToken);

        if (result.IsFailed)
            return BotResults.ShowView(new ScreenView($"Не удалось создать метрику: {BotErrorFormatter.Format(result.Errors)}").BackButton());

        ClearCreateSession(ctx);
        ctx.Session?.Data.Set(MetricManagementSessionKeys.SelectedMetricDefinitionId, result.Value.Id);

        return BotResults.NavigateTo<MetricsListScreen>();
    }

    private static Task<IEndpointResult> CancelCreateAsync(UpdateContext ctx)
    {
        ClearCreateSession(ctx);
        return Task.FromResult(BotResults.NavigateTo<MetricsListScreen>());
    }

    private static Task<IEndpointResult> StartEditAsync(UpdateContext ctx)
    {
        var metricId = ctx.Session?.Data.Get<Guid>(MetricManagementSessionKeys.SelectedMetricDefinitionId) ?? Guid.Empty;
        if (metricId == Guid.Empty)
            return Task.FromResult(BotResults.ShowView(new ScreenView("Метрика не выбрана.").BackButton()));

        ctx.Session?.Data.Set(
            MetricManagementSessionKeys.EditName,
            ctx.Session.Data.GetString(MetricManagementSessionKeys.SelectedMetricName) ?? string.Empty);
        ctx.Session?.Data.Set(
            MetricManagementSessionKeys.EditRedemptionAmount,
            ctx.Session.Data.Get<int>(MetricManagementSessionKeys.SelectedMetricRedemptionAmount));

        return Task.FromResult(BotResults.NavigateTo<EditMetricNameScreen>());
    }

    private static Task<IEndpointResult> EnterEditNameAsync(UpdateContext ctx)
    {
        var name = ctx.MessageText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            return Retry<EditMetricNameScreen, EnterEditMetricNameAction>("Название обязательно.");

        ctx.Session?.Data.Set(MetricManagementSessionKeys.EditName, name);
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.NavigateTo<EditMetricRedemptionAmountScreen>()));
    }

    private static Task<IEndpointResult> KeepEditNameAsync(UpdateContext ctx)
    {
        var current = ctx.Session?.Data.GetString(MetricManagementSessionKeys.SelectedMetricName) ?? string.Empty;
        ctx.Session?.Data.Set(MetricManagementSessionKeys.EditName, current);
        return Task.FromResult(BotResults.NavigateTo<EditMetricRedemptionAmountScreen>());
    }

    private static Task<IEndpointResult> EnterEditRedemptionAmountAsync(UpdateContext ctx)
    {
        var value = ctx.MessageText?.Trim() ?? string.Empty;

        if (!int.TryParse(value, out var amount) || amount <= 0)
            return Retry<EditMetricRedemptionAmountScreen, EnterEditMetricRedemptionAmountAction>("Количество для списания должно быть положительным числом.");

        ctx.Session?.Data.Set(MetricManagementSessionKeys.EditRedemptionAmount, amount);
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.NavigateTo<EditMetricConfirmScreen>()));
    }

    private static Task<IEndpointResult> KeepEditRedemptionAmountAsync(UpdateContext ctx)
    {
        var current = ctx.Session?.Data.Get<int>(MetricManagementSessionKeys.SelectedMetricRedemptionAmount) ?? 0;
        ctx.Session?.Data.Set(MetricManagementSessionKeys.EditRedemptionAmount, current);
        return Task.FromResult(BotResults.NavigateTo<EditMetricConfirmScreen>());
    }

    private static async Task<IEndpointResult> ConfirmEditAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        ICommandHandler<MetricResponse, UpdateMetricCommand> updateMetricHandler)
    {
        var metricId = ctx.Session?.Data.Get<Guid>(MetricManagementSessionKeys.SelectedMetricDefinitionId) ?? Guid.Empty;
        var name = ctx.Session?.Data.GetString(MetricManagementSessionKeys.EditName) ?? string.Empty;
        var redemptionAmount = ctx.Session?.Data.Get<int>(MetricManagementSessionKeys.EditRedemptionAmount) ?? 0;

        if (metricId == Guid.Empty || string.IsNullOrWhiteSpace(name) || redemptionAmount <= 0)
            return BotResults.ShowView(new ScreenView("Сценарий редактирования устарел. Начните заново.").BackButton());

        var userResult = await EnsureUserAsync(ctx, ensureUserHandler);
        if (userResult.IsFailed)
            return BotResults.ShowView(new ScreenView($"Не удалось определить пользователя: {BotErrorFormatter.Format(userResult.Errors)}").BackButton());

        var result = await updateMetricHandler.Handle(
            new UpdateMetricCommand(
                metricId,
                userResult.Value.UserId,
                new UpdateMetricRequest(name, redemptionAmount)),
            ctx.CancellationToken);

        if (result.IsFailed)
            return BotResults.ShowView(new ScreenView($"Не удалось сохранить метрику: {BotErrorFormatter.Format(result.Errors)}").BackButton());

        ClearEditSession(ctx);
        StoreSelectedMetric(ctx, result.Value);

        return BotResults.NavigateTo<MetricsListScreen>();
    }

    private static Task<IEndpointResult> CancelEditAsync(UpdateContext ctx)
    {
        ClearEditSession(ctx);
        return Task.FromResult(BotResults.NavigateTo<MetricDetailsScreen>());
    }

    private static async Task<FluentResults.Result<EnsureTelegramUserResponse>> EnsureUserAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler)
    {
        var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
        return await ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(ctx.UserId, from?.FirstName, from?.LastName, from?.Username),
            ctx.CancellationToken);
    }

    private static Task<IEndpointResult> Retry<TScreen, TAction>(string message)
        where TScreen : IScreen
        where TAction : IBotAction
    {
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(message)
            .AwaitInput<TAction>()
            .BackButton())));
    }

    private static void StoreSelectedMetric(UpdateContext ctx, MetricResponse metric)
    {
        ctx.Session?.Data.Set(MetricManagementSessionKeys.SelectedMetricDefinitionId, metric.Id);
        ctx.Session?.Data.Set(MetricManagementSessionKeys.SelectedMetricName, metric.Name);
        ctx.Session?.Data.Set(MetricManagementSessionKeys.SelectedMetricRedemptionAmount, metric.RedemptionAmount);
    }

    private static void ClearCreateSession(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(MetricManagementSessionKeys.CreateName);
        ctx.Session?.Data.Remove(MetricManagementSessionKeys.CreateRedemptionAmount);
    }

    private static void ClearEditSession(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(MetricManagementSessionKeys.EditName);
        ctx.Session?.Data.Remove(MetricManagementSessionKeys.EditRedemptionAmount);
    }
}
