using System.Text.Json;
using FluentResults;
using Microsoft.Extensions.Logging;
using StampService.Application.Abstractions;
using StampService.Application.Auth;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.Profile;
using StampService.Domain.User;

namespace StampService.Application.Users.Commands.ConfirmPhoneLinkCode;

public class ConfirmPhoneLinkCodeHandler
    : ICommandHandler<ConfirmPhoneLinkCodeResponse, ConfirmPhoneLinkCodeCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPhoneAuthCodeRepository _phoneAuthCodeRepository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ConfirmPhoneLinkCodeHandler> _logger;

    public ConfirmPhoneLinkCodeHandler(
        IUserRepository userRepository,
        IPhoneAuthCodeRepository phoneAuthCodeRepository,
        TimeProvider timeProvider,
        ILogger<ConfirmPhoneLinkCodeHandler> logger)
    {
        _userRepository = userRepository;
        _phoneAuthCodeRepository = phoneAuthCodeRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result<ConfirmPhoneLinkCodeResponse>> Handle(
        ConfirmPhoneLinkCodeCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        var phoneNumber = PhoneNumberNormalizer.Normalize(command.PhoneNumber);
        if (!PhoneAuthCode.IsValidPhoneNumber(phoneNumber))
            return Result.Fail(AuthErrors.PhoneInvalid(nameof(command.PhoneNumber)));

        var code = PhoneAuthCode.NormalizeCode(command.Code);
        if (!PhoneAuthCode.IsValidCode(code))
            return Result.Fail(AuthErrors.PhoneCodeInvalid());

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return Result.Fail(UserErrors.NotFound());

        if (user.Identities.Any(identity => identity.Type == IdentityType.Phone && identity.Key == phoneNumber))
        {
            return Result.Ok(new ConfirmPhoneLinkCodeResponse(
                phoneNumber,
                UserIdentityFormatter.MaskPhone(phoneNumber)));
        }

        var phoneOwner = await _userRepository.GetByIdentityAsync(
            IdentityType.Phone,
            phoneNumber,
            cancellationToken);
        if (phoneOwner is not null && phoneOwner.Id != command.UserId)
            return Result.Fail(UserErrors.IdentityLinkedToAnotherUser());

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var authCode = await GetActiveAuthCodeAsync(
            command.AuthCodeId,
            phoneNumber,
            nowUtc,
            cancellationToken);

        if (authCode is null)
        {
            _logger.LogWarning(
                "Phone link confirmation failed: active code not found. UserId={UserId} Phone={Phone} AuthCodeId={AuthCodeId}",
                command.UserId,
                phoneNumber,
                command.AuthCodeId);
            return Result.Fail(AuthErrors.PhoneCodeInvalid());
        }

        if (authCode.Code != code)
        {
            var failedAttemptResult = authCode.RegisterFailedAttempt(nowUtc);
            if (failedAttemptResult.IsSuccess)
            {
                try
                {
                    await _phoneAuthCodeRepository.SaveAsync(cancellationToken);
                }
                catch (ConcurrencyConflictException)
                {
                    return Result.Fail(AuthErrors.PhoneCodeInvalid());
                }
            }

            _logger.LogWarning(
                "Phone link confirmation failed: code mismatch. UserId={UserId} Phone={Phone} AuthCodeId={AuthCodeId}",
                command.UserId,
                phoneNumber,
                authCode.Id);
            return Result.Fail(AuthErrors.PhoneCodeInvalid());
        }

        var useResult = authCode.Use(nowUtc);
        if (useResult.IsFailed)
            return Result.Fail(AuthErrors.PhoneCodeInvalid());

        var metadata = JsonSerializer.Serialize(new
        {
            PhoneNumber = phoneNumber,
            LinkedAtUtc = nowUtc
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

            return Result.Fail(AuthErrors.PhoneCodeInvalid());
        }

        return Result.Ok(new ConfirmPhoneLinkCodeResponse(
            phoneNumber,
            UserIdentityFormatter.MaskPhone(phoneNumber)));
    }

    private async Task<PhoneAuthCode?> GetActiveAuthCodeAsync(
        Guid? authCodeId,
        string phoneNumber,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (authCodeId is { } id)
        {
            var authCode = await _phoneAuthCodeRepository.GetActiveByIdAsync(id, nowUtc, cancellationToken);
            if (authCode?.PhoneNumber == phoneNumber)
                return authCode;

            return null;
        }

        return await _phoneAuthCodeRepository.GetLatestActiveByPhoneAsync(
            phoneNumber,
            nowUtc,
            cancellationToken);
    }
}
