using StampService.Domain.User;

namespace StampService.Application.Auth;

public interface IPhoneAuthCodeRepository
{
    Task<IReadOnlyCollection<PhoneAuthCode>> GetActiveByPhoneAsync(
        string phoneNumber,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    Task<PhoneAuthCode?> GetLatestActiveByPhoneAsync(
        string phoneNumber,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    Task<PhoneAuthCode?> GetActiveByIdAsync(
        Guid id,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    void Add(PhoneAuthCode code);

    Task SaveAsync(CancellationToken cancellationToken);
}
