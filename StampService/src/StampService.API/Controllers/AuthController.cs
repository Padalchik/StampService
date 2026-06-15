using Microsoft.AspNetCore.Mvc;
using StampService.API.EndpointResults;
using StampService.Application.Auth;
using StampService.Contracts.DTOs.Auth;

namespace StampService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("telegram")]
    public async Task<EndpointResult<AuthResponse>> Login(
        TelegramLoginRequest request,
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        return await authService.LoginAsync(request, cancellationToken);
    }

    [HttpPost("phone/code")]
    public async Task<EndpointResult<RequestPhoneAuthCodeResponse>> RequestPhoneCode(
        RequestPhoneAuthCodeRequest request,
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        return await authService.RequestPhoneCodeAsync(request, cancellationToken);
    }

    [HttpGet("phone/sms-settings")]
    public async Task<EndpointResult<PhoneAuthSmsSettingsResponse>> GetPhoneSmsSettings(
        [FromServices] IPhoneAuthSmsSettingsRepository settingsRepository,
        CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetOrCreateAsync(cancellationToken);
        return FluentResults.Result.Ok(settings.ToResponse());
    }

    [HttpPost("phone/verify")]
    public async Task<EndpointResult<AuthResponse>> VerifyPhoneCode(
        VerifyPhoneAuthCodeRequest request,
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        return await authService.VerifyPhoneCodeAsync(request, cancellationToken);
    }
}
