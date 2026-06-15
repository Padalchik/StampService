using FluentResults;
using StampService.Application.Auth;

namespace StampService.Infrastructure.Services;

public sealed class CompositePhoneAuthCodeSender : IPhoneAuthCodeSender
{
    private readonly IReadOnlyCollection<IPhoneAuthCodeSender> _senders;

    public CompositePhoneAuthCodeSender(IReadOnlyCollection<IPhoneAuthCodeSender> senders)
    {
        _senders = senders;
    }

    public async Task<Result> SendAsync(
        string phoneNumber,
        string code,
        bool sendSms,
        CancellationToken cancellationToken)
    {
        foreach (var sender in _senders)
        {
            var result = await sender.SendAsync(phoneNumber, code, sendSms, cancellationToken);
            if (result.IsFailed)
                return result;
        }

        return Result.Ok();
    }
}
