using FluentResults;
using Microsoft.Extensions.Logging;
using StampService.Application.Abstractions;
using StampService.Application.Auth;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.Profile;
using StampService.Domain.User;

namespace StampService.Application.Users.Commands.RequestPhoneLinkCode;

public class RequestPhoneLinkCodeHandler
    : ICommandHandler<RequestPhoneLinkCodeResponse, RequestPhoneLinkCodeCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPhoneAuthCodeRepository _phoneAuthCodeRepository;
    private readonly IPhoneAuthCodeGenerator _phoneAuthCodeGenerator;
    private readonly IPhoneAuthCodeSender _phoneAuthCodeSender;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RequestPhoneLinkCodeHandler> _logger;

    public RequestPhoneLinkCodeHandler(
        IUserRepository userRepository,
        IPhoneAuthCodeRepository phoneAuthCodeRepository,
        IPhoneAuthCodeGenerator phoneAuthCodeGenerator,
        IPhoneAuthCodeSender phoneAuthCodeSender,
        TimeProvider timeProvider,
        ILogger<RequestPhoneLinkCodeHandler> logger)
    {
        _userRepository = userRepository;
        _phoneAuthCodeRepository = phoneAuthCodeRepository;
        _phoneAuthCodeGenerator = phoneAuthCodeGenerator;
        _phoneAuthCodeSender = phoneAuthCodeSender;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result<RequestPhoneLinkCodeResponse>> Handle(
        RequestPhoneLinkCodeCommand command,
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

        if (user.Identities.Any(identity =>
            identity.DeletedAt is null
            && identity.Type == IdentityType.Phone
            && identity.Key == phoneNumber))
            return Result.Fail(UserErrors.IdentityAlreadyLinked());

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var activeCodes = await _phoneAuthCodeRepository.GetActiveByPhoneAsync(
            phoneNumber,
            nowUtc,
            cancellationToken);
        foreach (var activeCode in activeCodes)
            activeCode.Expire(nowUtc);

        var expiresAtUtc = nowUtc.AddMinutes(10);
        var code = _phoneAuthCodeGenerator.Generate();
        var authCodeResult = PhoneAuthCode.Create(phoneNumber, code, expiresAtUtc, nowUtc);
        if (authCodeResult.IsFailed)
            return Result.Fail(authCodeResult.Errors);

        _phoneAuthCodeRepository.Add(authCodeResult.Value);
        await _phoneAuthCodeRepository.SaveAsync(cancellationToken);

        var sendResult = await _phoneAuthCodeSender.SendAsync(phoneNumber, code, cancellationToken);
        if (sendResult.IsFailed)
            return Result.Fail(sendResult.Errors);

        _logger.LogInformation(
            "Phone link code requested. UserId={UserId} Phone={Phone} AuthCodeId={AuthCodeId} ExpiresAtUtc={ExpiresAtUtc:o}",
            command.UserId,
            phoneNumber,
            authCodeResult.Value.Id,
            expiresAtUtc);

        return Result.Ok(new RequestPhoneLinkCodeResponse(expiresAtUtc, authCodeResult.Value.Id));
    }
}
