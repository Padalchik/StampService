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
    private readonly IPhoneAuthCodeService _phoneAuthCodeService;
    private readonly IPhoneAccountService _phoneAccountService;
    private readonly IPhoneAuthSmsSettingsRepository _phoneAuthSmsSettingsRepository;

    public AuthService(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        ITelegramValidationService telegramValidationService,
        IPhoneAuthCodeService phoneAuthCodeService,
        IPhoneAccountService phoneAccountService,
        IPhoneAuthSmsSettingsRepository phoneAuthSmsSettingsRepository)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _telegramValidationService = telegramValidationService;
        _phoneAuthCodeService = phoneAuthCodeService;
        _phoneAccountService = phoneAccountService;
        _phoneAuthSmsSettingsRepository = phoneAuthSmsSettingsRepository;
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
            return Result.Fail(UserErrors.TelegramIdentityNotLinked());

        if (!_phoneAccountService.HasActivePhoneIdentity(user))
            return Result.Fail(UserErrors.TelegramIdentityNotLinked());

        var token = _jwtTokenService.CreateToken(user);

        return Result.Ok(new AuthResponse(token.Value, user.Id, token.ExpiresAt));
    }

    public async Task<Result<RequestPhoneAuthCodeResponse>> RequestPhoneCodeAsync(
        RequestPhoneAuthCodeRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SendSms)
        {
            var smsSettings = await _phoneAuthSmsSettingsRepository.GetOrCreateAsync(cancellationToken);
            if (!smsSettings.IsEnabled)
                return Result.Fail(AuthErrors.PhoneSmsDisabled());
        }

        var requestResult = await _phoneAuthCodeService.RequestCodeAsync(
            request.PhoneNumber,
            nameof(request.PhoneNumber),
            cancellationToken,
            request.SendSms);
        if (requestResult.IsFailed)
            return Result.Fail(requestResult.Errors);

        return Result.Ok(new RequestPhoneAuthCodeResponse(requestResult.Value.ExpiresAtUtc));
    }

    public async Task<Result<AuthResponse>> VerifyPhoneCodeAsync(
        VerifyPhoneAuthCodeRequest request,
        CancellationToken cancellationToken)
    {
        var verificationResult = await _phoneAuthCodeService.VerifyCodeAsync(
            request.PhoneNumber,
            request.Code,
            authCodeId: null,
            invalidField: nameof(request.PhoneNumber),
            cancellationToken: cancellationToken);
        if (verificationResult.IsFailed)
            return Result.Fail(verificationResult.Errors);

        var userResult = await _phoneAccountService.GetOrCreateByPhoneAsync(
            verificationResult.Value.PhoneNumber,
            verificationResult.Value.VerifiedAtUtc,
            cancellationToken);
        if (userResult.IsFailed)
            return Result.Fail(userResult.Errors);

        try
        {
            await _userRepository.SaveAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Fail(AuthErrors.PhoneCodeInvalid());
        }

        var token = _jwtTokenService.CreateToken(userResult.Value);

        return Result.Ok(new AuthResponse(token.Value, userResult.Value.Id, token.ExpiresAt));
    }
}
