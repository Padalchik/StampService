using System.Text.Json;
using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Errors;
using StampService.Application.Services;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Auth;
using StampService.Contracts.DTOs.Profile;
using StampService.Domain.User;

namespace StampService.Application.Users.Commands.ConfirmTelegramLink;

public class ConfirmTelegramLinkHandler
    : ICommandHandler<ConfirmTelegramLinkResponse, ConfirmTelegramLinkCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ITelegramValidationService _telegramValidationService;
    private readonly IPhoneAccountService _phoneAccountService;

    public ConfirmTelegramLinkHandler(
        IUserRepository userRepository,
        ITelegramValidationService telegramValidationService,
        IPhoneAccountService phoneAccountService)
    {
        _userRepository = userRepository;
        _telegramValidationService = telegramValidationService;
        _phoneAccountService = phoneAccountService;
    }

    public async Task<Result<ConfirmTelegramLinkResponse>> Handle(
        ConfirmTelegramLinkCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        if (!_telegramValidationService.Validate(command.TelegramLogin))
            return Result.Fail(AuthErrors.TelegramLoginDataInvalid());

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return Result.Fail(UserErrors.NotFound());
        if (!_phoneAccountService.HasActivePhoneIdentity(user))
            return Result.Fail(UserErrors.TelegramIdentityNotLinked());

        var providerKey = command.TelegramLogin.Id.ToString();
        if (user.Identities.Any(identity =>
            identity.DeletedAt is null
            && identity.Type == IdentityType.Telegram
            && identity.Key == providerKey))
            return Result.Fail(UserErrors.IdentityAlreadyLinked());

        var telegramOwner = await _userRepository.GetByIdentityAsync(
            IdentityType.Telegram,
            providerKey,
            cancellationToken);
        if (telegramOwner is not null && telegramOwner.Id != command.UserId)
            return Result.Fail(UserErrors.IdentityLinkedToAnotherUser());

        var metadata = JsonSerializer.Serialize(new
        {
            command.TelegramLogin.Id,
            command.TelegramLogin.FirstName,
            command.TelegramLogin.LastName,
            command.TelegramLogin.Username,
            command.TelegramLogin.AuthDate
        });

        var identityResult = user.AddIdentity(IdentityType.Telegram, providerKey, metadata);
        if (identityResult.IsFailed)
            return Result.Fail(identityResult.Errors);

        await _userRepository.SaveAsync(cancellationToken);

        return Result.Ok(new ConfirmTelegramLinkResponse(
            command.TelegramLogin.Id,
            GetDisplayName(command.TelegramLogin)));
    }

    private static string GetDisplayName(TelegramLoginRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Username))
            return request.Username.Trim();

        var displayName = $"{request.FirstName} {request.LastName}".Trim();
        return string.IsNullOrWhiteSpace(displayName)
            ? request.Id.ToString()
            : displayName;
    }
}
