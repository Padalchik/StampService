using System.Text.Json;
using FluentResults;
using Microsoft.Extensions.Logging;
using StampService.Application.Abstractions;
using StampService.Application.Auth;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.Profile;
using StampService.Domain.User;

namespace StampService.Application.Users.Commands.ConfirmPhoneChangeCode;

public class ConfirmPhoneChangeCodeHandler
    : ICommandHandler<ConfirmPhoneLinkCodeResponse, ConfirmPhoneChangeCodeCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPhoneAuthCodeService _phoneAuthCodeService;
    private readonly ILogger<ConfirmPhoneChangeCodeHandler> _logger;

    public ConfirmPhoneChangeCodeHandler(
        IUserRepository userRepository,
        IPhoneAuthCodeService phoneAuthCodeService,
        ILogger<ConfirmPhoneChangeCodeHandler> logger)
    {
        _userRepository = userRepository;
        _phoneAuthCodeService = phoneAuthCodeService;
        _logger = logger;
    }

    public async Task<Result<ConfirmPhoneLinkCodeResponse>> Handle(
        ConfirmPhoneChangeCodeCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        var phoneNumberResult = PhoneNumberNormalizer.NormalizeForAuth(
            command.PhoneNumber,
            nameof(command.PhoneNumber));
        if (phoneNumberResult.IsFailed)
            return Result.Fail(phoneNumberResult.Errors);

        var phoneNumber = phoneNumberResult.Value;

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return Result.Fail(UserErrors.NotFound());

        var currentPhoneIdentity = user.Identities.FirstOrDefault(identity =>
            identity.DeletedAt is null
            && identity.Type == IdentityType.Phone);
        if (currentPhoneIdentity is null)
            return Result.Fail(UserErrors.IdentityNotLinked());

        if (currentPhoneIdentity.Key == phoneNumber)
            return Result.Fail(UserErrors.IdentityAlreadyLinked());

        var phoneOwner = await _userRepository.GetByIdentityAsync(
            IdentityType.Phone,
            phoneNumber,
            cancellationToken);
        if (phoneOwner is not null && phoneOwner.Id != command.UserId)
            return Result.Fail(UserErrors.IdentityLinkedToAnotherUser());

        var verificationResult = await _phoneAuthCodeService.VerifyCodeAsync(
            phoneNumber,
            command.Code,
            command.AuthCodeId,
            nameof(command.PhoneNumber),
            cancellationToken);
        if (verificationResult.IsFailed)
        {
            _logger.LogWarning(
                "Phone change confirmation failed. UserId={UserId} Phone={Phone} AuthCodeId={AuthCodeId} Errors={Errors}",
                command.UserId,
                phoneNumber,
                command.AuthCodeId,
                string.Join("; ", verificationResult.Errors.Select(error => error.Message)));
            return Result.Fail(verificationResult.Errors);
        }

        var changedAtUtc = verificationResult.Value.VerifiedAtUtc;
        var previousPhoneNumber = currentPhoneIdentity.Key;
        currentPhoneIdentity.Deactivate(changedAtUtc);

        var metadata = JsonSerializer.Serialize(new
        {
            PhoneNumber = phoneNumber,
            LinkedAtUtc = changedAtUtc,
            ChangedFromPhoneNumber = previousPhoneNumber
        });
        var identityResult = user.AddIdentity(IdentityType.Phone, phoneNumber, metadata);
        if (identityResult.IsFailed)
            return Result.Fail(identityResult.Errors);

        try
        {
            await _userRepository.SaveIdentityAsync(user, identityResult.Value, cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            var linkedUser = await _userRepository.GetByIdentityAsync(
                IdentityType.Phone,
                phoneNumber,
                cancellationToken);

            if (linkedUser?.Id == command.UserId)
            {
                return Result.Ok(new ConfirmPhoneLinkCodeResponse(
                    phoneNumber,
                    UserIdentityFormatter.MaskPhone(phoneNumber)));
            }

            return Result.Fail(UserErrors.IdentityLinkedToAnotherUser());
        }

        return Result.Ok(new ConfirmPhoneLinkCodeResponse(
            phoneNumber,
            UserIdentityFormatter.MaskPhone(phoneNumber)));
    }
}
