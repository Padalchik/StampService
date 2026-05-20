using Microsoft.Extensions.Logging;
using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Auth;
using StampService.Application.Users;
using StampService.Application.Users.Commands.ConfirmPhoneLinkCode;
using StampService.Application.Users.Commands.ConfirmTelegramPhoneCode;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Application.Users.Commands.RequestPhoneLinkCode;
using StampService.Contracts.DTOs.Profile;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Common.Routing;
using StampService.TelegramBot.Features.MainMenu.Screens;
using StampService.TelegramBot.Features.Profile.Actions;
using StampService.TelegramBot.Features.Profile.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Profile.Endpoints;

public sealed class ProfileEndpoint : IBotEndpoint
{
    private const string AuthenticatedPhoneLinkMode = "authenticated";
    private const string TelegramPhoneOnboardingMode = "telegram_phone_onboarding";

    public void MapEndpoint(BotApplication app)
    {
        app.MapAction<StartLinkPhoneAction>(StartLinkPhoneAsync);
        app.MapInput<EnterProfilePhoneAction>(EnterPhoneAsync);
        app.MapInput<EnterProfilePhoneCodeAction>(EnterCodeAsync);
    }

    private static Task<IEndpointResult> StartLinkPhoneAsync(UpdateContext ctx)
    {
        ClearLinkSession(ctx);
        return Task.FromResult(BotResults.NavigateTo<ProfilePhoneNumberScreen>());
    }

    private static async Task<IEndpointResult> EnterPhoneAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IPhoneAuthCodeService phoneAuthCodeService,
        ICommandHandler<RequestPhoneLinkCodeResponse, RequestPhoneLinkCodeCommand> requestCodeHandler,
        ILogger<ProfileEndpoint> logger)
    {
        var userResult = await BotEndpointHelpers.EnsureUserAsync(ctx, ensureUserHandler);
        var phoneNumber = ctx.MessageText?.Trim() ?? string.Empty;
        Result<RequestPhoneLinkCodeResponse> result;
        if (userResult.IsSuccess)
        {
            result = await requestCodeHandler.Handle(
                new RequestPhoneLinkCodeCommand(userResult.Value.UserId, phoneNumber),
                ctx.CancellationToken);
        }
        else
        {
            var requestResult = await phoneAuthCodeService.RequestCodeAsync(
                phoneNumber,
                invalidField: null,
                cancellationToken: ctx.CancellationToken);
            result = requestResult.IsFailed
                ? Result.Fail<RequestPhoneLinkCodeResponse>(requestResult.Errors)
                : Result.Ok(new RequestPhoneLinkCodeResponse(
                    requestResult.Value.ExpiresAtUtc,
                    requestResult.Value.AuthCodeId));
        }

        if (result.IsFailed)
        {
            logger.LogWarning(
                "Phone link code request failed from Telegram. TelegramUserId={TelegramUserId} UserId={UserId} Errors={Errors}",
                ctx.UserId,
                userResult.IsSuccess ? userResult.Value.UserId : (Guid?)null,
                string.Join("; ", result.Errors.Select(error => error.Message)));

            return await BotEndpointHelpers.RetryInput<ProfilePhoneNumberScreen, EnterProfilePhoneAction>(
                $"Не удалось отправить код: {BotErrorFormatter.Format(result.Errors)}");
        }

        ctx.Session?.Data.Set(ProfileSessionKeys.PhoneNumber, PhoneNumberNormalizer.Normalize(phoneNumber));
        if (userResult.IsSuccess)
        {
            ctx.Session?.Data.Set(ProfileSessionKeys.PhoneAuthCodeId, result.Value.AuthCodeId);
            ctx.Session?.Data.Set(ProfileSessionKeys.PhoneLinkMode, AuthenticatedPhoneLinkMode);
        }
        else
        {
            ctx.Session?.Data.Remove(ProfileSessionKeys.PhoneAuthCodeId);
            ctx.Session?.Data.Set(ProfileSessionKeys.PhoneLinkMode, TelegramPhoneOnboardingMode);
        }

        return BotInputResults.DeleteInputThen(BotResults.NavigateTo<ProfilePhoneCodeScreen>());
    }

    private static async Task<IEndpointResult> EnterCodeAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        ICommandHandler<ConfirmPhoneLinkCodeResponse, ConfirmPhoneLinkCodeCommand> confirmCodeHandler,
        ICommandHandler<EnsureTelegramUserResponse, ConfirmTelegramPhoneCodeCommand> confirmTelegramPhoneCodeHandler,
        ILogger<ProfileEndpoint> logger)
    {
        var phoneNumber = ctx.Session?.Data.GetString(ProfileSessionKeys.PhoneNumber) ?? string.Empty;
        var authCodeId = ctx.Session?.Data.Get<Guid>(ProfileSessionKeys.PhoneAuthCodeId);
        var phoneLinkMode = ctx.Session?.Data.GetString(ProfileSessionKeys.PhoneLinkMode);
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return BotResults.ShowView(new ScreenView("Сценарий привязки устарел. Начните заново.").BackButton());

        var userResult = await BotEndpointHelpers.EnsureUserAsync(ctx, ensureUserHandler);
        var code = ctx.MessageText?.Trim() ?? string.Empty;
        var isAuthenticatedPhoneLink = phoneLinkMode == AuthenticatedPhoneLinkMode
            || (phoneLinkMode is null && userResult.IsSuccess);
        Result<ConfirmPhoneLinkCodeResponse> result;
        if (isAuthenticatedPhoneLink && userResult.IsSuccess)
        {
            if (authCodeId is null || authCodeId == Guid.Empty)
                return BotResults.ShowView(new ScreenView("Сценарий привязки устарел. Начните заново.").BackButton());

            result = await confirmCodeHandler.Handle(
                new ConfirmPhoneLinkCodeCommand(userResult.Value.UserId, phoneNumber, code, authCodeId),
                ctx.CancellationToken);
        }
        else
        {
            var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
            var confirmResult = await confirmTelegramPhoneCodeHandler.Handle(
                new ConfirmTelegramPhoneCodeCommand(
                    ctx.UserId,
                    from?.FirstName,
                    from?.LastName,
                    from?.Username,
                    phoneNumber,
                    code),
                ctx.CancellationToken);
            result = confirmResult.IsFailed
                ? Result.Fail<ConfirmPhoneLinkCodeResponse>(confirmResult.Errors)
                : Result.Ok(new ConfirmPhoneLinkCodeResponse(
                    phoneNumber,
                    UserIdentityFormatter.MaskPhone(phoneNumber)));
        }

        if (result.IsFailed)
        {
            logger.LogWarning(
                "Phone link code confirmation failed from Telegram. TelegramUserId={TelegramUserId} UserId={UserId} AuthCodeId={AuthCodeId} Errors={Errors}",
                ctx.UserId,
                userResult.IsSuccess ? userResult.Value.UserId : (Guid?)null,
                authCodeId,
                string.Join("; ", result.Errors.Select(error => error.Message)));

            return await BotEndpointHelpers.RetryInput<ProfilePhoneCodeScreen, EnterProfilePhoneCodeAction>(
                $"Не удалось подтвердить код: {BotErrorFormatter.Format(result.Errors)}");
        }

        ClearLinkSession(ctx);

        return BotInputResults.DeleteInputThen(BotResults.ShowView(
            new ScreenView(
                "<b>Телефон привязан</b>\n\n" +
                $"К вашему профилю привязан номер {result.Value.MaskedPhoneNumber}.")
                .NavigateButton<MainMenuScreen>("Продолжить")));
    }

    private static void ClearLinkSession(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(ProfileSessionKeys.PhoneNumber);
        ctx.Session?.Data.Remove(ProfileSessionKeys.PhoneAuthCodeId);
        ctx.Session?.Data.Remove(ProfileSessionKeys.PhoneLinkMode);
    }
}
