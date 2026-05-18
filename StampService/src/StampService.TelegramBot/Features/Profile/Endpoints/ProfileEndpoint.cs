using Microsoft.Extensions.Logging;
using StampService.Application.Abstractions;
using StampService.Application.Users.Commands.ConfirmPhoneLinkCode;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Application.Users.Commands.RequestPhoneLinkCode;
using StampService.Contracts.DTOs.Profile;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Common.Routing;
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
        ICommandHandler<RequestPhoneLinkCodeResponse, RequestPhoneLinkCodeCommand> requestCodeHandler,
        ILogger<ProfileEndpoint> logger)
    {
        var userResult = await BotEndpointHelpers.EnsureUserAsync(ctx, ensureUserHandler);
        if (userResult.IsFailed)
            return BotResults.ShowView(new ScreenView("Не удалось определить пользователя.").BackButton());

        var phoneNumber = ctx.MessageText?.Trim() ?? string.Empty;
        var result = await requestCodeHandler.Handle(
            new RequestPhoneLinkCodeCommand(userResult.Value.UserId, phoneNumber),
            ctx.CancellationToken);

        if (result.IsFailed)
        {
            logger.LogWarning(
                "Phone link code request failed from Telegram. TelegramUserId={TelegramUserId} UserId={UserId} Errors={Errors}",
                ctx.UserId,
                userResult.Value.UserId,
                string.Join("; ", result.Errors.Select(error => error.Message)));

            return await BotEndpointHelpers.RetryInput<ProfilePhoneNumberScreen, EnterProfilePhoneAction>(
                $"Не удалось отправить код: {BotErrorFormatter.Format(result.Errors)}");
        }

        ctx.Session?.Data.Set(ProfileSessionKeys.PhoneNumber, phoneNumber);
        ctx.Session?.Data.Set(ProfileSessionKeys.PhoneAuthCodeId, result.Value.AuthCodeId);

        return BotInputResults.DeleteInputThen(BotResults.NavigateTo<ProfilePhoneCodeScreen>());
    }

    private static async Task<IEndpointResult> EnterCodeAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        ICommandHandler<ConfirmPhoneLinkCodeResponse, ConfirmPhoneLinkCodeCommand> confirmCodeHandler,
        ILogger<ProfileEndpoint> logger)
    {
        var phoneNumber = ctx.Session?.Data.GetString(ProfileSessionKeys.PhoneNumber) ?? string.Empty;
        var authCodeId = ctx.Session?.Data.Get<Guid>(ProfileSessionKeys.PhoneAuthCodeId);
        if (string.IsNullOrWhiteSpace(phoneNumber) || authCodeId is null || authCodeId == Guid.Empty)
            return BotResults.ShowView(new ScreenView("Сценарий привязки устарел. Начните заново.").BackButton());

        var userResult = await BotEndpointHelpers.EnsureUserAsync(ctx, ensureUserHandler);
        if (userResult.IsFailed)
            return BotResults.ShowView(new ScreenView("Не удалось определить пользователя.").BackButton());

        var code = ctx.MessageText?.Trim() ?? string.Empty;
        var result = await confirmCodeHandler.Handle(
            new ConfirmPhoneLinkCodeCommand(userResult.Value.UserId, phoneNumber, code, authCodeId),
            ctx.CancellationToken);

        if (result.IsFailed)
        {
            logger.LogWarning(
                "Phone link code confirmation failed from Telegram. TelegramUserId={TelegramUserId} UserId={UserId} AuthCodeId={AuthCodeId} Errors={Errors}",
                ctx.UserId,
                userResult.Value.UserId,
                authCodeId,
                string.Join("; ", result.Errors.Select(error => error.Message)));

            return await BotEndpointHelpers.RetryInput<ProfilePhoneCodeScreen, EnterProfilePhoneCodeAction>(
                $"Не удалось подтвердить код: {BotErrorFormatter.Format(result.Errors)}");
        }

        ClearLinkSession(ctx);

        return BotInputResults.DeleteInputThen(BotResults.ShowView(
            new ScreenView(
                "<b>Телефон привязан</b>\n\n" +
                $"Теперь к вашему профилю привязан номер {result.Value.MaskedPhoneNumber}.")
                .NavigateButton<ProfileScreen>("Открыть личный кабинет")));
    }

    private static void ClearLinkSession(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(ProfileSessionKeys.PhoneNumber);
        ctx.Session?.Data.Remove(ProfileSessionKeys.PhoneAuthCodeId);
    }
}
