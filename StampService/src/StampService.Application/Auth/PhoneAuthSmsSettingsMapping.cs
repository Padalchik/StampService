using StampService.Contracts.DTOs.Auth;
using StampService.Domain.User;

namespace StampService.Application.Auth;

public static class PhoneAuthSmsSettingsMapping
{
    public static PhoneAuthSmsSettingsResponse ToResponse(this PhoneAuthSmsSettings settings)
    {
        return new PhoneAuthSmsSettingsResponse(settings.IsEnabled);
    }
}
