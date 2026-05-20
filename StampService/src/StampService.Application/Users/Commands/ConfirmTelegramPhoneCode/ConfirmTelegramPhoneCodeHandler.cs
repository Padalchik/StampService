using System.Text.Json;
using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Auth;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Domain.User;

namespace StampService.Application.Users.Commands.ConfirmTelegramPhoneCode;

public class ConfirmTelegramPhoneCodeHandler
    : ICommandHandler<EnsureTelegramUserResponse, ConfirmTelegramPhoneCodeCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPhoneAuthCodeService _phoneAuthCodeService;
    private readonly IPhoneAccountService _phoneAccountService;

    public ConfirmTelegramPhoneCodeHandler(
        IUserRepository userRepository,
        IPhoneAuthCodeService phoneAuthCodeService,
        IPhoneAccountService phoneAccountService)
    {
        _userRepository = userRepository;
        _phoneAuthCodeService = phoneAuthCodeService;
        _phoneAccountService = phoneAccountService;
    }

    public async Task<Result<EnsureTelegramUserResponse>> Handle(
        ConfirmTelegramPhoneCodeCommand command,
        CancellationToken cancellationToken)
    {
        if (command.TelegramUserId <= 0)
            return Result.Fail(UserErrors.TelegramUserIdMustBePositive());

        var phoneNumberResult = PhoneNumberNormalizer.NormalizeForAuth(
            command.PhoneNumber,
            nameof(command.PhoneNumber));
        if (phoneNumberResult.IsFailed)
            return Result.Fail(phoneNumberResult.Errors);

        var phoneNumber = phoneNumberResult.Value;
        var providerKey = command.TelegramUserId.ToString();
        var telegramOwner = await _userRepository.GetByIdentityAsync(
            IdentityType.Telegram,
            providerKey,
            cancellationToken);
        var phoneOwner = await _userRepository.GetByIdentityAsync(
            IdentityType.Phone,
            phoneNumber,
            cancellationToken);

        if (telegramOwner is not null
            && (phoneOwner is null || phoneOwner.Id != telegramOwner.Id))
            return Result.Fail(UserErrors.IdentityLinkedToAnotherUser());

        var phoneOwnerTelegramIdentity = phoneOwner?.Identities.FirstOrDefault(identity =>
            identity.DeletedAt is null && identity.Type == IdentityType.Telegram);
        if (phoneOwnerTelegramIdentity is not null && phoneOwnerTelegramIdentity.Key != providerKey)
            return Result.Fail(UserErrors.IdentityAlreadyLinked());

        var verificationResult = await _phoneAuthCodeService.VerifyCodeAsync(
            phoneNumber,
            command.Code,
            authCodeId: null,
            nameof(command.PhoneNumber),
            cancellationToken);
        if (verificationResult.IsFailed)
            return Result.Fail(verificationResult.Errors);

        var userResult = await _phoneAccountService.GetOrCreateByPhoneAsync(
            phoneNumber,
            verificationResult.Value.VerifiedAtUtc,
            cancellationToken);
        if (userResult.IsFailed)
            return Result.Fail(userResult.Errors);

        var user = userResult.Value;
        var sameTelegramIdentity = user.Identities.FirstOrDefault(identity =>
            identity.DeletedAt is null
            && identity.Type == IdentityType.Telegram
            && identity.Key == providerKey);

        if (sameTelegramIdentity is null)
        {
            var currentTelegramIdentity = user.Identities.FirstOrDefault(identity =>
                identity.DeletedAt is null && identity.Type == IdentityType.Telegram);
            if (currentTelegramIdentity is not null)
                return Result.Fail(UserErrors.IdentityAlreadyLinked());

            var displayName = GetDisplayName(command);
            var metadata = JsonSerializer.Serialize(new
            {
                Id = command.TelegramUserId,
                command.FirstName,
                command.LastName,
                command.Username,
                DisplayName = displayName,
                LinkedAtUtc = verificationResult.Value.VerifiedAtUtc
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
                return Result.Fail(AuthErrors.PhoneCodeInvalid());
            }

            return Result.Ok(new EnsureTelegramUserResponse(
                user.Id,
                Created: false,
                user.Name,
                user.CustomerCode));
        }

        try
        {
            await _userRepository.SaveAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Result.Fail(AuthErrors.PhoneCodeInvalid());
        }

        return Result.Ok(new EnsureTelegramUserResponse(
            user.Id,
            Created: false,
            user.Name,
            user.CustomerCode));
    }

    private static string GetDisplayName(ConfirmTelegramPhoneCodeCommand command)
    {
        if (!string.IsNullOrWhiteSpace(command.Username))
            return command.Username.Trim();

        var name = $"{command.FirstName} {command.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name)
            ? command.TelegramUserId.ToString()
            : name;
    }
}
