using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.Brands.Commands.AddBrandStaffByCustomerCode;
using StampService.Application.Brands.Commands.RemoveBrandStaff;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Brands;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Common.Routing;
using StampService.TelegramBot.Features.Staff.Actions;
using StampService.TelegramBot.Features.Staff.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;
using UserEntity = StampService.Domain.User.User;

namespace StampService.TelegramBot.Features.Staff.Endpoints;

public sealed class BrandStaffEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapAction<OpenBrandStaffAction, OpenBrandStaffPayload>(OpenBrandStaffAsync);
        app.MapAction<StartAddStaffAction>(StartAddStaffAsync);
        app.MapInput<EnterAddStaffCustomerCodeAction>(EnterAddStaffCustomerCodeAsync);
        app.MapAction<ConfirmAddStaffAction>(ConfirmAddStaffAsync);
        app.MapAction<CancelAddStaffAction>(CancelAddStaffAsync);
        app.MapAction<OpenStaffDetailsAction, OpenStaffDetailsPayload>(OpenStaffDetailsAsync);
        app.MapAction<StartRemoveStaffAction>(StartRemoveStaffAsync);
        app.MapAction<ConfirmRemoveStaffAction>(ConfirmRemoveStaffAsync);
        app.MapAction<CancelRemoveStaffAction>(CancelRemoveStaffAsync);
    }

    private static Task<IEndpointResult> OpenBrandStaffAsync(
        UpdateContext ctx,
        OpenBrandStaffPayload payload)
    {
        ctx.Session?.Data.Set(StaffSessionKeys.BrandId, payload.BrandId);
        ctx.Session?.Data.Set(StaffSessionKeys.BrandName, payload.BrandName);
        ClearSelectedStaff(ctx);
        ctx.Session?.Data.Remove(StaffSessionKeys.AddCustomerCode);

        return Task.FromResult(BotResults.NavigateTo<BrandStaffListScreen>());
    }

    private static Task<IEndpointResult> StartAddStaffAsync(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(StaffSessionKeys.AddCustomerCode);
        return Task.FromResult(BotResults.NavigateTo<AddStaffCustomerCodeScreen>());
    }

    private static Task<IEndpointResult> EnterAddStaffCustomerCodeAsync(UpdateContext ctx)
    {
        var customerCode = ctx.MessageText?.Trim() ?? string.Empty;
        if (!UserEntity.IsValidCustomerCode(customerCode))
        {
            return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(
                "CustomerCode должен состоять из 4 цифр.")
                .AwaitInput<EnterAddStaffCustomerCodeAction>()
                .BackButton())));
        }

        ctx.Session?.Data.Set(StaffSessionKeys.AddCustomerCode, customerCode);
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.NavigateTo<AddStaffConfirmScreen>()));
    }

    private static async Task<IEndpointResult> ConfirmAddStaffAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        ICommandHandler<AddBrandStaffByCustomerCodeResponse, AddBrandStaffByCustomerCodeCommand> addStaffHandler)
    {
        var brandId = StaffBrandContext.GetBrandId(ctx);
        var customerCode = ctx.Session?.Data.GetString(StaffSessionKeys.AddCustomerCode) ?? string.Empty;

        if (brandId == Guid.Empty || !UserEntity.IsValidCustomerCode(customerCode))
            return BotResults.ShowView(new ScreenView("Сценарий добавления сотрудника устарел. Начните заново.").BackButton());

        var actorUserId = await GetActorUserIdAsync(ctx, ensureUserHandler);
        if (actorUserId is null)
            return BotResults.ShowView(new ScreenView("Не удалось определить пользователя.").BackButton());

        var result = await addStaffHandler.Handle(
            new AddBrandStaffByCustomerCodeCommand(actorUserId.Value, brandId, customerCode),
            ctx.CancellationToken);

        if (result.IsFailed)
            return BotResults.ShowView(new ScreenView($"Не удалось добавить сотрудника: {BotErrorFormatter.Format(result.Errors)}").BackButton());

        ctx.Session?.Data.Remove(StaffSessionKeys.AddCustomerCode);

        return BotResults.ShowView(new ScreenView(
            "<b>Сотрудник добавлен</b>\n\n" +
            $"{Html(result.Value.UserName)} · <code>{Html(result.Value.CustomerCode)}</code>")
            .NavigateButton<BrandStaffListScreen>("К сотрудникам")
            .BackButton());
    }

    private static Task<IEndpointResult> CancelAddStaffAsync(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(StaffSessionKeys.AddCustomerCode);
        return Task.FromResult(BotResults.NavigateTo<BrandStaffListScreen>());
    }

    private static Task<IEndpointResult> OpenStaffDetailsAsync(
        UpdateContext ctx,
        OpenStaffDetailsPayload payload)
    {
        ctx.Session?.Data.Set(StaffSessionKeys.SelectedStaffUserId, payload.UserId);
        ctx.Session?.Data.Set(StaffSessionKeys.SelectedStaffName, payload.UserName);
        ctx.Session?.Data.Set(StaffSessionKeys.SelectedStaffCustomerCode, payload.CustomerCode);

        return Task.FromResult(BotResults.NavigateTo<StaffDetailsScreen>());
    }

    private static Task<IEndpointResult> StartRemoveStaffAsync(UpdateContext ctx)
    {
        var staffUserId = ctx.Session?.Data.Get<Guid>(StaffSessionKeys.SelectedStaffUserId) ?? Guid.Empty;
        if (staffUserId == Guid.Empty)
            return Task.FromResult(BotResults.ShowView(new ScreenView("Сотрудник не выбран.").BackButton()));

        return Task.FromResult(BotResults.NavigateTo<RemoveStaffConfirmScreen>());
    }

    private static async Task<IEndpointResult> ConfirmRemoveStaffAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        ICommandHandler<RemoveBrandStaffResponse, RemoveBrandStaffCommand> removeStaffHandler)
    {
        var brandId = StaffBrandContext.GetBrandId(ctx);
        var staffUserId = ctx.Session?.Data.Get<Guid>(StaffSessionKeys.SelectedStaffUserId) ?? Guid.Empty;

        if (brandId == Guid.Empty || staffUserId == Guid.Empty)
            return BotResults.ShowView(new ScreenView("Сценарий удаления сотрудника устарел. Начните заново.").BackButton());

        var actorUserId = await GetActorUserIdAsync(ctx, ensureUserHandler);
        if (actorUserId is null)
            return BotResults.ShowView(new ScreenView("Не удалось определить пользователя.").BackButton());

        var result = await removeStaffHandler.Handle(
            new RemoveBrandStaffCommand(actorUserId.Value, brandId, staffUserId),
            ctx.CancellationToken);

        if (result.IsFailed)
            return BotResults.ShowView(new ScreenView($"Не удалось удалить сотрудника: {BotErrorFormatter.Format(result.Errors)}").BackButton());

        ClearSelectedStaff(ctx);

        return BotResults.ShowView(new ScreenView(
            "<b>Сотрудник удалён</b>\n\n" +
            $"{Html(result.Value.UserName)} · <code>{Html(result.Value.CustomerCode)}</code>")
            .NavigateButton<BrandStaffListScreen>("К сотрудникам")
            .BackButton());
    }

    private static Task<IEndpointResult> CancelRemoveStaffAsync(UpdateContext ctx)
    {
        return Task.FromResult(BotResults.NavigateTo<StaffDetailsScreen>());
    }

    private static async Task<Guid?> GetActorUserIdAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler)
    {
        var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
        var result = await ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(
                ctx.UserId,
                from?.FirstName,
                from?.LastName,
                from?.Username),
            ctx.CancellationToken);

        return result.IsSuccess ? result.Value.UserId : null;
    }

    private static void ClearSelectedStaff(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(StaffSessionKeys.SelectedStaffUserId);
        ctx.Session?.Data.Remove(StaffSessionKeys.SelectedStaffName);
        ctx.Session?.Data.Remove(StaffSessionKeys.SelectedStaffCustomerCode);
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
