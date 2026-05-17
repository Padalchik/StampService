using FluentResults;
using StampService.Contracts.DTOs.Auth;

namespace StampService.Application.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> LoginAsync(TelegramLoginRequest request, CancellationToken cancellationToken);

    Task<Result<RequestPhoneAuthCodeResponse>> RequestPhoneCodeAsync(
        RequestPhoneAuthCodeRequest request,
        CancellationToken cancellationToken);

    Task<Result<AuthResponse>> VerifyPhoneCodeAsync(
        VerifyPhoneAuthCodeRequest request,
        CancellationToken cancellationToken);
}
