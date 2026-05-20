using StampService.Domain.User;

namespace StampService.Application.Users;

public static class UserIdentityFormatter
{
    public static string MaskPhone(string phoneNumber)
    {
        if (!PhoneNumber.IsValidNormalized(phoneNumber))
            return phoneNumber;

        var suffix = phoneNumber[^4..];
        return $"{phoneNumber[..2]}******{suffix}";
    }
}
