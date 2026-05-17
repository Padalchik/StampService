namespace StampService.Application.Auth;

public static class PhoneNumberNormalizer
{
    public static string Normalize(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return string.Empty;

        var trimmed = phoneNumber.Trim();
        var hasPlus = trimmed.StartsWith('+');
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());

        return hasPlus
            ? $"+{digits}"
            : digits;
    }
}
