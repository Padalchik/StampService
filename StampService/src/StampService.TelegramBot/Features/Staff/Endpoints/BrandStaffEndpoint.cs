using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.Auth;
using StampService.Application.Brands.Commands.AddBrandStaffByPhone;
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

namespace StampService.TelegramBot.Features.Staff.Endpoints;

public sealed class BrandStaffEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapAction<OpenBrandStaffAction, OpenBrandStaffPayload>(OpenBrandStaffAsync);
        app.MapAction<StartAddStaffAction>(StartAddStaffAsync);
        app.MapInput<EnterAddStaffPhoneAction>(EnterAddStaffPhoneAsync);
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
        ctx.Session?.Data.Remove(StaffSessionKeys.AddPhoneNumber);

        return Task.FromResult(BotResults.NavigateTo<BrandStaffListScreen>());
    }

    private static Task<IEndpointResult> StartAddStaffAsync(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(StaffSessionKeys.AddPhoneNumber);
        return Task.FromResult(BotResults.NavigateTo<AddStaffPhoneScreen>());
    }

    private static Task<IEndpointResult> EnterAddStaffPhoneAsync(UpdateContext ctx)
    {
        var phoneNumberResult = PhoneNumberNormalizer.NormalizeForAuth(ctx.MessageText, "PhoneNumber");
        if (phoneNumberResult.IsFailed)
        {
            return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(
                "Введите корректный номер телефона.")
                .AwaitInput<EnterAddStaffPhoneAction>()
                .BackButton())));
        }

        ctx.Session?.Data.Set(StaffSessionKeys.AddPhoneNumber, phoneNumberResult.Value);
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.NavigateTo<AddStaffConfirmScreen>()));
    }

    private static async Task<IEndpointResult> ConfirmAddStaffAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        ICommandHandler<AddBrandStaffByPhoneResponse, AddBrandStaffByPhoneCommand> addStaffHandler)
    {
        var brandId = StaffBrandContext.GetBrandId(ctx);
        var phoneNumber = ctx.Session?.Data.GetString(StaffSessionKeys.AddPhoneNumber) ?? string.Empty;

        if (brandId == Guid.Empty || PhoneNumberNormalizer.NormalizeForAuth(phoneNumber).IsFailed)
            return BotResults.ShowView(new ScreenView("Сценарий добавления сотрудника устарел. Начните заново.").BackButton());

        var actorUserId = await GetActorUserIdAsync(ctx, ensureUserHandler);
        if (actorUserId is null)
            return BotResults.ShowView(new ScreenView("Не удалось определить пользователя.").BackButton());

        var result = await addStaffHandler.Handle(
            new AddBrandStaffByPhoneCommand(actorUserId.Value, brandId, phoneNumber),
            ctx.CancellationToken);

        if (result.IsFailed)
            return BotResults.ShowView(new ScreenView($"Не удалось добавить сотрудника: {BotErrorFormatter.Format(result.Errors)}").BackButton());

        ctx.Session?.Data.Remove(StaffSessionKeys.AddPhoneNumber);
        ClearSelectedStaff(ctx);

        return BotResults.ShowView(new ScreenView(
            "<b>Сотрудник добавлен</b>\n\n" +
            $"{Html(result.Value.UserName)} · <code>{Html(result.Value.PhoneNumber)}</code>")
            .NavigateButton<BrandStaffListScreen>("К сотрудникам")
            .BackButton());
    }

    private static Task<IEndpointResult> CancelAddStaffAsync(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(StaffSessionKeys.AddPhoneNumber);
        return Task.FromResult(BotResults.NavigateTo<BrandStaffListScreen>());
    }

    private static Task<IEndpointResult> OpenStaffDetailsAsync(
        UpdateContext ctx,
        OpenStaffDetailsPayload payload)
    {
        ctx.Session?.Data.Set(StaffSessionKeys.SelectedStaffUserId, payload.UserId);
        ctx.Session?.Data.Set(StaffSessionKeys.SelectedStaffName, payload.UserName);
        SetOrRemove(ctx, StaffSessionKeys.SelectedStaffPhoneNumber, payload.PhoneNumber);

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

        ctx.Session?.Data.Remove(StaffSessionKeys.AddPhoneNumber);
        ClearSelectedStaff(ctx);

        return BotResults.ShowView(new ScreenView(
            "<b>Сотрудник удалён</b>\n\n" +
            $"{Html(result.Value.UserName)} · <code>{Html(DisplayPhone(result.Value.PhoneNumber))}</code>")
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
        ctx.Session?.Data.Remove(StaffSessionKeys.SelectedStaffPhoneNumber);
    }

    private static void SetOrRemove(UpdateContext ctx, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            ctx.Session?.Data.Remove(key);
        else
            ctx.Session?.Data.Set(key, value);
    }

    private static string DisplayPhone(string? phoneNumber) => string.IsNullOrWhiteSpace(phoneNumber) ? "-" : phoneNumber;

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
