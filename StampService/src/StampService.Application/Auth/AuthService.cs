using System.Text.Json;
using FluentResults;
using StampService.Application.Errors;
using StampService.Application.Services;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Auth;
using StampService.Domain.User;

namespace StampService.Application.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITelegramValidationService _telegramValidationService;
    private readonly ICustomerCodeGenerator _customerCodeGenerator;
    private readonly IPhoneAuthCodeRepository _phoneAuthCodeRepository;
    private readonly IPhoneAuthCodeGenerator _phoneAuthCodeGenerator;
    private readonly IPhoneAuthCodeSender _phoneAuthCodeSender;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        ITelegramValidationService telegramValidationService,
        ICustomerCodeGenerator customerCodeGenerator,
        IPhoneAuthCodeRepository phoneAuthCodeRepository,
        IPhoneAuthCodeGenerator phoneAuthCodeGenerator,
        IPhoneAuthCodeSender phoneAuthCodeSender,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _telegramValidationService = telegramValidationService;
        _customerCodeGenerator = customerCodeGenerator;
        _phoneAuthCodeRepository = phoneAuthCodeRepository;
        _phoneAuthCodeGenerator = phoneAuthCodeGenerator;
        _phoneAuthCodeSender = phoneAuthCodeSender;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AuthResponse>> LoginAsync(
        TelegramLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!_telegramValidationService.Validate(request))
            return Result.Fail(AuthErrors.TelegramLoginDataInvalid());

        var providerKey = request.Id.ToString();
        var user = await _userRepository.GetByIdentityAsync(
            IdentityType.Telegram,
            providerKey,
            cancellationToken);

        if (user is null)
        {
            var customerCode = await _customerCodeGenerator.GenerateAsync(cancellationToken);
            var userResult = User.Create(GetDisplayName(request), customerCode);
            if (userResult.IsFailed)
                return Result.Fail(userResult.Errors);

            user = userResult.Value;

            var metadata = JsonSerializer.Serialize(new
            {
                request.Id,
                request.FirstName,
                request.LastName,
                request.Username,
                request.AuthDate
            });

            var identityResult = user.AddIdentity(IdentityType.Telegram, providerKey, metadata);
            if (identityResult.IsFailed)
                return Result.Fail(identityResult.Errors);

            _userRepository.Add(user);
            await _userRepository.SaveAsync(cancellationToken);
        }

        var token = _jwtTokenService.CreateToken(user);

        return Result.Ok(new AuthResponse(token.Value, user.Id, token.ExpiresAt));
    }

    public async Task<Result<RequestPhoneAuthCodeResponse>> RequestPhoneCodeAsync(
        RequestPhoneAuthCodeRequest request,
        CancellationToken cancellationToken)
    {
        var phoneNumber = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        if (!PhoneAuthCode.IsValidPhoneNumber(phoneNumber))
            return Result.Fail(AuthErrors.PhoneInvalid(nameof(request.PhoneNumber)));

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

        return Result.Ok(new RequestPhoneAuthCodeResponse(expiresAtUtc));
    }

    public async Task<Result<AuthResponse>> VerifyPhoneCodeAsync(
        VerifyPhoneAuthCodeRequest request,
        CancellationToken cancellationToken)
    {
        var phoneNumber = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        if (!PhoneAuthCode.IsValidPhoneNumber(phoneNumber))
            return Result.Fail(AuthErrors.PhoneInvalid(nameof(request.PhoneNumber)));

        var code = PhoneAuthCode.NormalizeCode(request.Code);
        if (!PhoneAuthCode.IsValidCode(code))
            return Result.Fail(AuthErrors.PhoneCodeInvalid());

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var authCode = await _phoneAuthCodeRepository.GetLatestActiveByPhoneAsync(
            phoneNumber,
            nowUtc,
            cancellationToken);
        if (authCode is null)
            return Result.Fail(AuthErrors.PhoneCodeInvalid());

        if (authCode.Code != code)
        {
            var failedAttemptResult = authCode.RegisterFailedAttempt(nowUtc);
            if (failedAttemptResult.IsFailed)
                return Result.Fail(AuthErrors.PhoneCodeInvalid());

            try
            {
                await _phoneAuthCodeRepository.SaveAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                return Result.Fail(AuthErrors.PhoneCodeInvalid());
            }

            return Result.Fail(AuthErrors.PhoneCodeInvalid());
        }

        var useResult = authCode.Use(nowUtc);
        if (useResult.IsFailed)
            return Result.Fail(AuthErrors.PhoneCodeInvalid());

        var user = await _userRepository.GetByIdentityAsync(
            IdentityType.Phone,
            phoneNumber,
            cancellationToken);

        if (user is null)
        {
            var customerCode = await _customerCodeGenerator.GenerateAsync(cancellationToken);
            var userResult = User.Create(phoneNumber, customerCode);
            if (userResult.IsFailed)
                return Result.Fail(userResult.Errors);

            user = userResult.Value;
            var metadata = JsonSerializer.Serialize(new
            {
                PhoneNumber = phoneNumber,
                VerifiedAtUtc = nowUtc
            });

            var identityResult = user.AddIdentity(IdentityType.Phone, phoneNumber, metadata);
            if (identityResult.IsFailed)
                return Result.Fail(identityResult.Errors);

            _userRepository.Add(user);
        }

        try
        {
            await _userRepository.SaveAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Fail(AuthErrors.PhoneCodeInvalid());
        }

        var token = _jwtTokenService.CreateToken(user);

        return Result.Ok(new AuthResponse(token.Value, user.Id, token.ExpiresAt));
    }

    private static string GetDisplayName(TelegramLoginRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Username))
            return request.Username.Trim();

        return $"{request.FirstName} {request.LastName}".Trim();
    }
}
