using FluentResults;

namespace StampService.Application.Auth;

public interface IPhoneAuthCodeService
{
    Task<Result<PhoneAuthCodeRequestResult>> RequestCodeAsync(
        string phoneNumber,
        string? invalidField,
        CancellationToken cancellationToken);

    Task<Result<PhoneAuthCodeVerificationResult>> VerifyCodeAsync(
        string phoneNumber,
        string code,
        Guid? authCodeId,
        string? invalidField,
        CancellationToken cancellationToken);
}
