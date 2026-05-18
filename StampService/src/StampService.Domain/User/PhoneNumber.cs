using FluentResults;
using StampService.Domain.Shared;

namespace StampService.Domain.User;

public sealed record PhoneNumber
{
    public const int MinDigitsLength = 2;
    public const int MaxDigitsLength = 15;
    public const int MaxInputLength = 32;
    public const int CountryCodeSevenDigitsLength = 11;

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<PhoneNumber> Create(string? input)
    {
        var normalizedResult = Normalize(input);
        if (normalizedResult.IsFailed)
            return Result.Fail(normalizedResult.Errors);

        return Result.Ok(new PhoneNumber(normalizedResult.Value));
    }

    public static Result<string> Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Invalid(nameof(input));

        var trimmed = input.Trim();
        if (trimmed.Length > MaxInputLength)
            return Invalid(nameof(input));

        if (trimmed[0] != '+')
            return Invalid(nameof(input));

        if (trimmed.Count(character => character == '+') != 1)
            return Invalid(nameof(input));

        if (trimmed.Skip(1).Any(character => !IsAllowedAfterPlus(character)))
            return Invalid(nameof(input));

        var digits = new string(trimmed.Where(IsAsciiDigit).ToArray());
        if (digits.Length is < MinDigitsLength or > MaxDigitsLength)
            return Invalid(nameof(input));

        if (digits[0] == '0')
            return Invalid(nameof(input));

        if (digits[0] == '7' && digits.Length != CountryCodeSevenDigitsLength)
            return Invalid(nameof(input));

        return Result.Ok($"+{digits}");
    }

    public static bool IsValidNormalized(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (value.Length > MaxInputLength || value[0] != '+')
            return false;

        var digits = value[1..];
        if (digits.Length is < MinDigitsLength or > MaxDigitsLength)
            return false;

        if (!digits.All(IsAsciiDigit) || digits[0] == '0')
            return false;

        return digits[0] != '7' || digits.Length == CountryCodeSevenDigitsLength;
    }

    public override string ToString() => Value;

    private static bool IsAllowedAfterPlus(char character)
    {
        return IsAsciiDigit(character)
            || character == ' '
            || character == '-'
            || character == '('
            || character == ')';
    }

    private static bool IsAsciiDigit(char character) => character is >= '0' and <= '9';

    private static Result<string> Invalid(string field)
    {
        return Result.Fail(DomainError.Validation(
            "phone_number.invalid",
            "Phone number is invalid",
            field));
    }
}
