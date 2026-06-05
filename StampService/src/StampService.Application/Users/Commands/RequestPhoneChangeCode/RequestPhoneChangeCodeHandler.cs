using FluentResults;
using Microsoft.Extensions.Logging;
using StampService.Application.Abstractions;
using StampService.Application.Auth;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.Profile;
using StampService.Domain.User;

namespace StampService.Application.Users.Commands.RequestPhoneChangeCode;

public class RequestPhoneChangeCodeHandler
    : ICommandHandler<RequestPhoneLinkCodeResponse, RequestPhoneChangeCodeCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPhoneAuthCodeService _phoneAuthCodeService;
    private readonly ILogger<RequestPhoneChangeCodeHandler> _logger;

    public RequestPhoneChangeCodeHandler(
        IUserRepository userRepository,
        IPhoneAuthCodeService phoneAuthCodeService,
        ILogger<RequestPhoneChangeCodeHandler> logger)
    {
        _userRepository = userRepository;
        _phoneAuthCodeService = phoneAuthCodeService;
        _logger = logger;
    }

    public async Task<Result<RequestPhoneLinkCodeResponse>> Handle(
        RequestPhoneChangeCodeCommand command,
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

        var requestResult = await _phoneAuthCodeService.RequestCodeAsync(
            phoneNumber,
            nameof(command.PhoneNumber),
            cancellationToken);
        if (requestResult.IsFailed)
            return Result.Fail(requestResult.Errors);

        _logger.LogInformation(
            "Phone change code requested. UserId={UserId} Phone={Phone} AuthCodeId={AuthCodeId} ExpiresAtUtc={ExpiresAtUtc:o}",
            command.UserId,
            requestResult.Value.PhoneNumber,
            requestResult.Value.AuthCodeId,
            requestResult.Value.ExpiresAtUtc);

        return Result.Ok(new RequestPhoneLinkCodeResponse(
            requestResult.Value.ExpiresAtUtc,
            requestResult.Value.AuthCodeId));
    }
}
