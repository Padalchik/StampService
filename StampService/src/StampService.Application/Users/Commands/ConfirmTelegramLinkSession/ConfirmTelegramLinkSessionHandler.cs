using System.Text.Json;
using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.Profile;
using StampService.Domain.User;

namespace StampService.Application.Users.Commands.ConfirmTelegramLinkSession;

public class ConfirmTelegramLinkSessionHandler
    : ICommandHandler<ConfirmTelegramLinkResponse, ConfirmTelegramLinkSessionCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ITelegramLinkSessionProtector _protector;
    private readonly TimeProvider _timeProvider;

    public ConfirmTelegramLinkSessionHandler(
        IUserRepository userRepository,
        ITelegramLinkSessionProtector protector,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _protector = protector;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ConfirmTelegramLinkResponse>> Handle(
        ConfirmTelegramLinkSessionCommand command,
        CancellationToken cancellationToken)
    {
        if (command.TelegramUserId <= 0)
            return Result.Fail(UserErrors.TelegramUserIdMustBePositive());

        var sessionResult = _protector.Unprotect(command.Token);
        if (sessionResult.IsFailed)
            return Result.Fail(AuthErrors.TelegramCodeInvalid());

        var session = sessionResult.Value;
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        if (session.ExpiresAtUtc <= nowUtc)
            return Result.Fail(AuthErrors.TelegramCodeInvalid());

        var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken);
        if (user is null)
            return Result.Fail(UserErrors.NotFound());

        var providerKey = command.TelegramUserId.ToString();
        var sameTelegramIdentity = user.Identities.FirstOrDefault(identity =>
            identity.Type == IdentityType.Telegram && identity.Key == providerKey);
        var displayName = GetDisplayName(command);
        if (sameTelegramIdentity is not null)
            return Result.Ok(new ConfirmTelegramLinkResponse(command.TelegramUserId, displayName));

        var telegramOwner = await _userRepository.GetByIdentityAsync(
            IdentityType.Telegram,
            providerKey,
            cancellationToken);
        if (telegramOwner is not null && telegramOwner.Id != session.UserId)
            return Result.Fail(UserErrors.IdentityLinkedToAnotherUser());

        var currentTelegramIdentity = user.Identities.FirstOrDefault(identity => identity.Type == IdentityType.Telegram);
        currentTelegramIdentity?.Deactivate(nowUtc);

        var metadata = JsonSerializer.Serialize(new
        {
            Id = command.TelegramUserId,
            command.FirstName,
            command.LastName,
            command.Username,
            DisplayName = displayName,
            LinkedAtUtc = nowUtc
        });
        var identityResult = user.AddIdentity(IdentityType.Telegram, providerKey, metadata);
        if (identityResult.IsFailed)
            return Result.Fail(identityResult.Errors);

        try
        {
            await _userRepository.SaveIdentityAsync(user, identityResult.Value, cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            var linkedUser = await _userRepository.GetByIdentityAsync(
                IdentityType.Telegram,
                providerKey,
                cancellationToken);

            if (linkedUser?.Id == session.UserId)
                return Result.Ok(new ConfirmTelegramLinkResponse(command.TelegramUserId, displayName));

            return Result.Fail(AuthErrors.TelegramCodeInvalid());
        }

        return Result.Ok(new ConfirmTelegramLinkResponse(command.TelegramUserId, displayName));
    }

    private static string GetDisplayName(ConfirmTelegramLinkSessionCommand command)
    {
        if (!string.IsNullOrWhiteSpace(command.Username))
            return command.Username.Trim();

        var fullName = $"{command.FirstName} {command.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName)
            ? command.TelegramUserId.ToString()
            : fullName;
    }
}

