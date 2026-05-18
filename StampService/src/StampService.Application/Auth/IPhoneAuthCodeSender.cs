using FluentResults;

namespace StampService.Application.Auth;

public interface IPhoneAuthCodeSender
{
    Task<Result> SendAsync(string phoneNumber, string code, CancellationToken cancellationToken);
}
