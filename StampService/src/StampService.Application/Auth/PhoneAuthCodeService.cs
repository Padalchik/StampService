using FluentResults;
using StampService.Application.Errors;
using StampService.Domain.User;

namespace StampService.Application.Auth;

public class PhoneAuthCodeService : IPhoneAuthCodeService
{
    private readonly IPhoneAuthCodeRepository _phoneAuthCodeRepository;
    private readonly IPhoneAuthCodeGenerator _phoneAuthCodeGenerator;
    private readonly IPhoneAuthCodeSender _phoneAuthCodeSender;
    private readonly TimeProvider _timeProvider;

    public PhoneAuthCodeService(
        IPhoneAuthCodeRepository phoneAuthCodeRepository,
        IPhoneAuthCodeGenerator phoneAuthCodeGenerator,
        IPhoneAuthCodeSender phoneAuthCodeSender,
        TimeProvider timeProvider)
    {
        _phoneAuthCodeRepository = phoneAuthCodeRepository;
        _phoneAuthCodeGenerator = phoneAuthCodeGenerator;
        _phoneAuthCodeSender = phoneAuthCodeSender;
        _timeProvider = timeProvider;
    }

    public async Task<Result<PhoneAuthCodeRequestResult>> RequestCodeAsync(
        string phoneNumber,
        string? invalidField,
        CancellationToken cancellationToken,
        bool sendSms = false)
    {
        var phoneNumberResult = PhoneNumberNormalizer.NormalizeForAuth(phoneNumber, invalidField);
        if (phoneNumberResult.IsFailed)
            return Result.Fail(phoneNumberResult.Errors);

        var normalizedPhoneNumber = phoneNumberResult.Value;
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var activeCodes = await _phoneAuthCodeRepository.GetActiveByPhoneAsync(
            normalizedPhoneNumber,
            nowUtc,
            cancellationToken);
        foreach (var activeCode in activeCodes)
            activeCode.Expire(nowUtc);

        var expiresAtUtc = nowUtc.AddMinutes(10);
        var code = _phoneAuthCodeGenerator.Generate();
        var authCodeResult = PhoneAuthCode.Create(normalizedPhoneNumber, code, expiresAtUtc, nowUtc);
        if (authCodeResult.IsFailed)
            return Result.Fail(authCodeResult.Errors);

        _phoneAuthCodeRepository.Add(authCodeResult.Value);
        await _phoneAuthCodeRepository.SaveAsync(cancellationToken);

        var sendResult = await _phoneAuthCodeSender.SendAsync(normalizedPhoneNumber, code, sendSms, cancellationToken);
        if (sendResult.IsFailed)
            return Result.Fail(sendResult.Errors);

        return Result.Ok(new PhoneAuthCodeRequestResult(
            normalizedPhoneNumber,
            expiresAtUtc,
            authCodeResult.Value.Id));
    }

    public async Task<Result<PhoneAuthCodeVerificationResult>> VerifyCodeAsync(
        string phoneNumber,
        string code,
        Guid? authCodeId,
        string? invalidField,
        CancellationToken cancellationToken)
    {
        var phoneNumberResult = PhoneNumberNormalizer.NormalizeForAuth(phoneNumber, invalidField);
        if (phoneNumberResult.IsFailed)
            return Result.Fail(phoneNumberResult.Errors);

        var normalizedPhoneNumber = phoneNumberResult.Value;
        var normalizedCode = PhoneAuthCode.NormalizeCode(code);
        if (!PhoneAuthCode.IsValidCode(normalizedCode))
            return Result.Fail(AuthErrors.PhoneCodeInvalid());

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var authCode = await GetActiveAuthCodeAsync(
            authCodeId,
            normalizedPhoneNumber,
            nowUtc,
            cancellationToken);
        if (authCode is null)
            return Result.Fail(AuthErrors.PhoneCodeInvalid());

        if (authCode.Code != normalizedCode)
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

            return Result.Fail(AuthErrors.PhoneCodeInvalid());
        }

        var useResult = authCode.Use(nowUtc);
        if (useResult.IsFailed)
            return Result.Fail(AuthErrors.PhoneCodeInvalid());

        return Result.Ok(new PhoneAuthCodeVerificationResult(normalizedPhoneNumber, nowUtc));
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
            return authCode?.PhoneNumber == phoneNumber ? authCode : null;
        }

        return await _phoneAuthCodeRepository.GetLatestActiveByPhoneAsync(
            phoneNumber,
            nowUtc,
            cancellationToken);
    }
}
