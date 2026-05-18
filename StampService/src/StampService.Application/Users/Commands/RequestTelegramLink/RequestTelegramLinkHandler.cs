using FluentResults;
using Microsoft.Extensions.Options;
using StampService.Application.Abstractions;
using StampService.Application.Errors;
using StampService.Application.Services;
using StampService.Contracts.DTOs.Profile;

namespace StampService.Application.Users.Commands.RequestTelegramLink;

public class RequestTelegramLinkHandler
    : ICommandHandler<RequestTelegramLinkResponse, RequestTelegramLinkCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ITelegramLinkSessionProtector _protector;
    private readonly TimeProvider _timeProvider;
    private readonly TelegramOptions _telegramOptions;

    public RequestTelegramLinkHandler(
        IUserRepository userRepository,
        ITelegramLinkSessionProtector protector,
        TimeProvider timeProvider,
        IOptions<TelegramOptions> telegramOptions)
    {
        _userRepository = userRepository;
        _protector = protector;
        _timeProvider = timeProvider;
        _telegramOptions = telegramOptions.Value;
    }

    public async Task<Result<RequestTelegramLinkResponse>> Handle(
        RequestTelegramLinkCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        var userExists = await _userRepository.ExistsAsync(command.UserId, cancellationToken);
        if (!userExists)
            return Result.Fail(UserErrors.NotFound());

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
