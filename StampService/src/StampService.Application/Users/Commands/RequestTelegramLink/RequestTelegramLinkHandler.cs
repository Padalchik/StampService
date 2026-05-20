using FluentResults;
using Microsoft.Extensions.Options;
using StampService.Application.Abstractions;
using StampService.Application.Errors;
using StampService.Application.Services;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Profile;
using StampService.Domain.User;

namespace StampService.Application.Users.Commands.RequestTelegramLink;

public class RequestTelegramLinkHandler
    : ICommandHandler<RequestTelegramLinkResponse, RequestTelegramLinkCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ITelegramLinkSessionProtector _protector;
    private readonly TimeProvider _timeProvider;
    private readonly TelegramOptions _telegramOptions;
    private readonly IPhoneAccountService _phoneAccountService;

    public RequestTelegramLinkHandler(
        IUserRepository userRepository,
        ITelegramLinkSessionProtector protector,
        TimeProvider timeProvider,
        IOptions<TelegramOptions> telegramOptions,
        IPhoneAccountService phoneAccountService)
    {
        _userRepository = userRepository;
        _protector = protector;
        _timeProvider = timeProvider;
        _telegramOptions = telegramOptions.Value;
        _phoneAccountService = phoneAccountService;
    }

    public async Task<Result<RequestTelegramLinkResponse>> Handle(
        RequestTelegramLinkCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return Result.Fail(UserErrors.NotFound());
        if (!_phoneAccountService.HasActivePhoneIdentity(user))
            return Result.Fail(UserErrors.TelegramIdentityNotLinked());
        if (user.Identities.Any(identity =>
            identity.DeletedAt is null && identity.Type == IdentityType.Telegram))
            return Result.Fail(UserErrors.IdentityAlreadyLinked());

        var botUsername = GetBotUsername();
        if (string.IsNullOrWhiteSpace(botUsername))
            return Result.Fail(AuthErrors.TelegramCodeSendFailed("Telegram:BotUsername is not configured"));

        var expiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(10);
        var token = _protector.Protect(new TelegramLinkSession(command.UserId, expiresAtUtc));
        var url = $"https://t.me/{botUsername}?start={Uri.EscapeDataString(token)}";

        return Result.Ok(new RequestTelegramLinkResponse(url, expiresAtUtc));
    }

    private string? GetBotUsername()
    {
        var value = _telegramOptions.BotUsername;
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().TrimStart('@');
    }
}
