using System.Net;
using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Application.Users.Queries.GetMyProfile;
using StampService.Contracts.DTOs.Profile;
using StampService.TelegramBot.Common.UI;
using StampService.TelegramBot.Features.Profile.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Profile.Screens;

public sealed class ProfileScreen : IScreen
{
    private readonly ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> _ensureUserHandler;
    private readonly IQueryHandler<MyProfileResponse, GetMyProfileQuery> _profileHandler;

    public ProfileScreen(
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<MyProfileResponse, GetMyProfileQuery> profileHandler)
    {
        _ensureUserHandler = ensureUserHandler;
        _profileHandler = profileHandler;
    }

    public async ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var userResult = await EnsureUserAsync(ctx);
        if (userResult.IsFailed)
            return new ScreenView("Не удалось определить пользователя.").BackButton();

        var profileResult = await _profileHandler.Handle(
            new GetMyProfileQuery(userResult.Value.UserId),
            ctx.CancellationToken);
        if (profileResult.IsFailed)
            return new ScreenView("Не удалось загрузить профиль.").BackButton();

        var profile = profileResult.Value;
        var phoneText = profile.Phone.Linked
            ? Html(profile.Phone.Value ?? "привязан")
            : "не привязан";
        var telegramText = profile.Telegram.Linked
            ? Html(profile.Telegram.Value ?? "привязан")
            : "не привязан";

        var view = new ScreenView(
            $"<b>{BotMenuLabels.AccountSettings}</b>\n\n" +
            $"Имя: {Html(profile.DisplayName)}\n" +
            $"Телефон: {phoneText}\n" +
            $"Telegram: {telegramText}");

        view.Row().Button<StartLinkPhoneAction>(profile.Phone.Linked ? "Изменить телефон" : "Привязать телефон");

        return view.BackButton();
    }

    private Task<Result<EnsureTelegramUserResponse>> EnsureUserAsync(UpdateContext ctx)
    {
        var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
        return _ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(
                ctx.UserId,
                from?.FirstName,
                from?.LastName,
                from?.Username),
            ctx.CancellationToken);
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
