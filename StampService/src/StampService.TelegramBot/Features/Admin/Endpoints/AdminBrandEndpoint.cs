using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.Brands.Commands.CreateBrandWithOwner;
using StampService.Application.Brands.Commands.ReassignBrandOwner;
using StampService.Contracts.DTOs.Brands;
using StampService.Domain.User;
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

public sealed class AdminBrandEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapAction<OpenAdminBrandAction, OpenAdminBrandPayload>(OpenBrandAsync);
        app.MapAction<StartCreateBrandAction>(StartCreateBrandAsync);
        app.MapInput<EnterCreateBrandNameAction>(EnterCreateBrandNameAsync);
        app.MapInput<EnterCreateBrandOwnerCodeAction>(EnterCreateBrandOwnerCodeAsync);
        app.MapAction<ConfirmCreateBrandAction>(ConfirmCreateBrandAsync);
        app.MapAction<CancelCreateBrandAction>(CancelCreateBrandAsync);
        app.MapAction<StartReassignOwnerAction>(StartReassignOwnerAsync);
        app.MapInput<EnterReassignOwnerCodeAction>(EnterReassignOwnerCodeAsync);
        app.MapAction<ConfirmReassignOwnerAction>(ConfirmReassignOwnerAsync);
        app.MapAction<CancelReassignOwnerAction>(CancelReassignOwnerAsync);
    }

    private static Task<IEndpointResult> OpenBrandAsync(
        UpdateContext ctx,
        OpenAdminBrandPayload payload)
    {
        ctx.Session?.Data.Set(AdminSessionKeys.SelectedBrandId, payload.BrandId);
        ctx.Session?.Data.Set(AdminSessionKeys.SelectedBrandName, payload.BrandName);
        ctx.Session?.Data.Set(AdminSessionKeys.SelectedBrandMetricsEnabled, payload.IsMetricsEnabled);
        ctx.Session?.Data.Set(AdminSessionKeys.SelectedBrandCoinsEnabled, payload.IsCoinsEnabled);

        if (payload.OwnerUserId is { } ownerUserId)
            ctx.Session?.Data.Set(AdminSessionKeys.SelectedOwnerUserId, ownerUserId);
        else
            ctx.Session?.Data.Remove(AdminSessionKeys.SelectedOwnerUserId);

        SetOrRemove(ctx, AdminSessionKeys.SelectedOwnerName, payload.OwnerName);
        SetOrRemove(ctx, AdminSessionKeys.SelectedOwnerCustomerCode, payload.OwnerCustomerCode);

        return Task.FromResult(BotResults.NavigateTo<AdminBrandDetailsScreen>());
    }

    private static Task<IEndpointResult> StartCreateBrandAsync(UpdateContext ctx)
    {
        ClearCreateSession(ctx);
        return Task.FromResult(BotResults.NavigateTo<CreateBrandNameScreen>());
    }

    private static Task<IEndpointResult> EnterCreateBrandNameAsync(UpdateContext ctx)
    {
        var brandName = ctx.MessageText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(brandName))
            return Retry<CreateBrandNameScreen, EnterCreateBrandNameAction>("Название бренда обязательно.");

        ctx.Session?.Data.Set(AdminSessionKeys.CreateBrandName, brandName);
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.NavigateTo<CreateBrandOwnerCodeScreen>()));
    }

    private static Task<IEndpointResult> EnterCreateBrandOwnerCodeAsync(UpdateContext ctx)
    {
        var ownerCode = ctx.MessageText?.Trim() ?? string.Empty;
        if (!User.IsValidCustomerCode(ownerCode))
            return Retry<CreateBrandOwnerCodeScreen, EnterCreateBrandOwnerCodeAction>("Код пользователя должен состоять из 4 цифр.");

        ctx.Session?.Data.Set(AdminSessionKeys.CreateOwnerCustomerCode, ownerCode);
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.NavigateTo<CreateBrandConfirmScreen>()));
    }

    private static async Task<IEndpointResult> ConfirmCreateBrandAsync(
        UpdateContext ctx,
        ICommandHandler<CreateBrandWithOwnerResponse, CreateBrandWithOwnerCommand> handler)
    {
        var brandName = ctx.Session?.Data.GetString(AdminSessionKeys.CreateBrandName) ?? string.Empty;
        var ownerCode = ctx.Session?.Data.GetString(AdminSessionKeys.CreateOwnerCustomerCode) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(brandName) || !User.IsValidCustomerCode(ownerCode))
            return BotResults.ShowView(new ScreenView("Сценарий создания бренда устарел. Начните заново.").BackButton());

        var result = await handler.Handle(
            new CreateBrandWithOwnerCommand(ctx.UserId, brandName, ownerCode),
            ctx.CancellationToken);

        if (result.IsFailed)
            return BotResults.ShowView(new ScreenView($"Не удалось создать бренд: {BotErrorFormatter.Format(result.Errors)}").BackButton());

        ClearCreateSession(ctx);
        StoreSelectedBrand(
            ctx,
            result.Value.BrandId,
            result.Value.BrandName,
            result.Value.IsMetricsEnabled,
            result.Value.IsCoinsEnabled,
            result.Value.OwnerUserId,
            result.Value.OwnerName,
            result.Value.OwnerCustomerCode);

        return BotResults.ShowView(new ScreenView(
            "<b>Бренд создан</b>\n\n" +
            $"{Html(result.Value.BrandName)}\n" +
            $"Владелец: {Html(result.Value.OwnerName)} · <code>{Html(result.Value.OwnerCustomerCode)}</code>")
            .NavigateButton<AdminBrandDetailsScreen>("Открыть бренд")
            .Row()
            .NavigateButton<AdminPanelScreen>("К админке"));
    }

    private static Task<IEndpointResult> CancelCreateBrandAsync(UpdateContext ctx)
    {
        ClearCreateSession(ctx);
        return Task.FromResult(BotResults.NavigateTo<AdminPanelScreen>());
    }

    private static Task<IEndpointResult> StartReassignOwnerAsync(UpdateContext ctx)
    {
        var brandId = ctx.Session?.Data.Get<Guid>(AdminSessionKeys.SelectedBrandId) ?? Guid.Empty;
        if (brandId == Guid.Empty)
            return Task.FromResult(BotResults.ShowView(new ScreenView("Бренд не выбран.").BackButton()));

        ctx.Session?.Data.Remove(AdminSessionKeys.ReassignOwnerCustomerCode);
        return Task.FromResult(BotResults.NavigateTo<ReassignOwnerCodeScreen>());
    }

    private static Task<IEndpointResult> EnterReassignOwnerCodeAsync(UpdateContext ctx)
    {
        var ownerCode = ctx.MessageText?.Trim() ?? string.Empty;
        if (!User.IsValidCustomerCode(ownerCode))
            return Retry<ReassignOwnerCodeScreen, EnterReassignOwnerCodeAction>("Код пользователя должен состоять из 4 цифр.");

        ctx.Session?.Data.Set(AdminSessionKeys.ReassignOwnerCustomerCode, ownerCode);
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.NavigateTo<ReassignOwnerConfirmScreen>()));
    }

    private static async Task<IEndpointResult> ConfirmReassignOwnerAsync(
        UpdateContext ctx,
        ICommandHandler<ReassignBrandOwnerResponse, ReassignBrandOwnerCommand> handler)
    {
        var brandId = ctx.Session?.Data.Get<Guid>(AdminSessionKeys.SelectedBrandId) ?? Guid.Empty;
        var brandName = ctx.Session?.Data.GetString(AdminSessionKeys.SelectedBrandName) ?? "бренд";
        var ownerCode = ctx.Session?.Data.GetString(AdminSessionKeys.ReassignOwnerCustomerCode) ?? string.Empty;

        if (brandId == Guid.Empty || !User.IsValidCustomerCode(ownerCode))
            return BotResults.ShowView(new ScreenView("Сценарий смены владельца устарел. Начните заново.").BackButton());

        var result = await handler.Handle(
            new ReassignBrandOwnerCommand(ctx.UserId, brandId, ownerCode),
            ctx.CancellationToken);

        if (result.IsFailed)
            return BotResults.ShowView(new ScreenView($"Не удалось сменить владельца: {BotErrorFormatter.Format(result.Errors)}").BackButton());

        ctx.Session?.Data.Remove(AdminSessionKeys.ReassignOwnerCustomerCode);
        StoreSelectedBrand(
            ctx,
            brandId,
            brandName,
            ctx.Session?.Data.Get<bool>(AdminSessionKeys.SelectedBrandMetricsEnabled) ?? true,
            ctx.Session?.Data.Get<bool>(AdminSessionKeys.SelectedBrandCoinsEnabled) ?? true,
            result.Value.NewOwnerUserId,
            result.Value.NewOwnerName,
            result.Value.NewOwnerCustomerCode);

        var removedOwnerText = result.Value.RemovedOwnerUserId is null
            ? string.Empty
            : "\nСтарый владелец удалён из бренда.";

        return BotResults.ShowView(new ScreenView(
            "<b>Владелец обновлён</b>\n\n" +
            $"Новый владелец: {Html(result.Value.NewOwnerName)} · <code>{Html(result.Value.NewOwnerCustomerCode)}</code>" +
            removedOwnerText)
            .NavigateButton<AdminBrandDetailsScreen>("Открыть бренд")
            .Row()
            .NavigateButton<AdminPanelScreen>("К админке"));
    }

    private static Task<IEndpointResult> CancelReassignOwnerAsync(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(AdminSessionKeys.ReassignOwnerCustomerCode);
        return Task.FromResult(BotResults.NavigateTo<AdminBrandDetailsScreen>());
    }

    private static Task<IEndpointResult> Retry<TScreen, TAction>(string message)
        where TScreen : IScreen
        where TAction : IBotAction
    {
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(message)
            .AwaitInput<TAction>()
            .BackButton())));
    }

    private static void StoreSelectedBrand(
        UpdateContext ctx,
        Guid brandId,
        string brandName,
        bool isMetricsEnabled,
        bool isCoinsEnabled,
        Guid ownerUserId,
        string ownerName,
        string ownerCustomerCode)
    {
        StoreSelectedBrandSettings(ctx, brandId, brandName, isMetricsEnabled, isCoinsEnabled);
        ctx.Session?.Data.Set(AdminSessionKeys.SelectedOwnerUserId, ownerUserId);
        ctx.Session?.Data.Set(AdminSessionKeys.SelectedOwnerName, ownerName);
        ctx.Session?.Data.Set(AdminSessionKeys.SelectedOwnerCustomerCode, ownerCustomerCode);
    }

    private static void StoreSelectedBrandSettings(
        UpdateContext ctx,
        Guid brandId,
        string brandName,
        bool isMetricsEnabled,
        bool isCoinsEnabled)
    {
        ctx.Session?.Data.Set(AdminSessionKeys.SelectedBrandId, brandId);
        ctx.Session?.Data.Set(AdminSessionKeys.SelectedBrandName, brandName);
        ctx.Session?.Data.Set(AdminSessionKeys.SelectedBrandMetricsEnabled, isMetricsEnabled);
        ctx.Session?.Data.Set(AdminSessionKeys.SelectedBrandCoinsEnabled, isCoinsEnabled);
    }

    private static void ClearCreateSession(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(AdminSessionKeys.CreateBrandName);
        ctx.Session?.Data.Remove(AdminSessionKeys.CreateOwnerCustomerCode);
    }

    private static void SetOrRemove(UpdateContext ctx, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            ctx.Session?.Data.Remove(key);
        else
            ctx.Session?.Data.Set(key, value);
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);

    private static string FormatEnabled(bool value) => value ? "включены" : "выключены";
}
