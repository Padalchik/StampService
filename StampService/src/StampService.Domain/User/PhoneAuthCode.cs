using FluentResults;
using StampService.Domain.Shared;

namespace StampService.Domain.User;

public class PhoneAuthCode : BaseEntity
{
    public const int CodeLength = 6;
    public const int MaxPhoneLength = global::StampService.Domain.User.PhoneNumber.MaxInputLength;
    public const int MaxAttempts = 5;

    public string PhoneNumber { get; private set; }
    public string Code { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }
    public int FailedAttempts { get; private set; }

    private PhoneAuthCode(string phoneNumber, string code, DateTime expiresAtUtc)
    {
        PhoneNumber = phoneNumber;
        Code = code;
        ExpiresAtUtc = expiresAtUtc;
    }

    // EF Core
    protected PhoneAuthCode()
    {
        PhoneNumber = null!;
        Code = null!;
    }

    public static Result<PhoneAuthCode> Create(
        string phoneNumber,
        string code,
        DateTime expiresAtUtc,
        DateTime nowUtc)
    {
        var phoneNumberResult = global::StampService.Domain.User.PhoneNumber.Create(phoneNumber);
        if (phoneNumberResult.IsFailed)
            return Result.Fail(phoneNumberResult.Errors);

        var normalizedCode = NormalizeCode(code);
        if (!IsValidCode(normalizedCode))
            return Result.Fail(DomainError.Validation(
                "phone_auth_code.code_invalid",
                $"Phone auth code must contain exactly {CodeLength} digits",
                nameof(code)));

        if (expiresAtUtc <= nowUtc)
            return Result.Fail(DomainError.Validation(
                "phone_auth_code.expires_at_invalid",
                "Phone auth code expiration date must be in the future",
                nameof(expiresAtUtc)));

        return Result.Ok(new PhoneAuthCode(phoneNumberResult.Value.Value, normalizedCode, expiresAtUtc));
    }

    public Result Use(DateTime nowUtc)
    {
        if (UsedAtUtc is not null)
            return Result.Fail(DomainError.Conflict(
                "phone_auth_code.already_used",
                "Phone auth code has already been used"));

        if (!IsActive(nowUtc))
            return Result.Fail(DomainError.Validation(
                "phone_auth_code.expired",
                "Phone auth code has expired"));

        UsedAtUtc = nowUtc;
        Touch();

        return Result.Ok();
    }

    public Result RegisterFailedAttempt(DateTime nowUtc)
    {
        if (!IsActive(nowUtc))
            return Result.Fail(DomainError.Validation(
                "phone_auth_code.expired",
                "Phone auth code has expired"));

        FailedAttempts++;
        if (FailedAttempts >= MaxAttempts)
            ExpiresAtUtc = nowUtc;

        Touch();

        return Result.Ok();
    }

    public void Expire(DateTime nowUtc)
    {
        if (UsedAtUtc is null && ExpiresAtUtc > nowUtc)
        {
            ExpiresAtUtc = nowUtc;
            Touch();
        }
    }

    public bool IsActive(DateTime nowUtc)
    {
        return UsedAtUtc is null
            && ExpiresAtUtc > nowUtc
            && FailedAttempts < MaxAttempts;
    }

    public static bool IsValidCode(string? code)
    {
        var normalizedCode = NormalizeCode(code);

        return normalizedCode.Length == CodeLength
            && normalizedCode.All(char.IsDigit);
    }

    public static string NormalizeCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        return new string(code.Where(c => c is >= '0' and <= '9').ToArray());
    }

    public static bool IsValidPhoneNumber(string? phoneNumber)
    {
        return global::StampService.Domain.User.PhoneNumber.IsValidNormalized(phoneNumber);
    }
}
