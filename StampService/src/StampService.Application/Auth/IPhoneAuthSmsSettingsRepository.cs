using StampService.Domain.User;

namespace StampService.Application.Auth;

public interface IPhoneAuthSmsSettingsRepository
{
    Task<PhoneAuthSmsSettings> GetOrCreateAsync(CancellationToken cancellationToken);

    Task SaveAsync(CancellationToken cancellationToken);
}
