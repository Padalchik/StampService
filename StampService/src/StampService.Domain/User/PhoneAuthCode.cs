using FluentResults;
using StampService.Domain.Shared;

namespace StampService.Domain.User;

public class PhoneAuthCode : BaseEntity
{
    public const int CodeLength = 6;
    public const int MaxPhoneLength = 32;
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
        if (!IsValidPhoneNumber(phoneNumber))
            return Result.Fail(DomainError.Validation(
                "phone_auth_code.phone_invalid",
                "Phone number is invalid",
                nameof(phoneNumber)));

        if (!IsValidCode(code))
            return Result.Fail(DomainError.Validation(
                "phone_auth_code.code_invalid",
                $"Phone auth code must contain exactly {CodeLength} digits",
                nameof(code)));

        if (expiresAtUtc <= nowUtc)
            return Result.Fail(DomainError.Validation(
                "phone_auth_code.expires_at_invalid",
                "Phone auth code expiration date must be in the future",
                nameof(expiresAtUtc)));

        return Result.Ok(new PhoneAuthCode(phoneNumber, code, expiresAtUtc));
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
        return code is not null
            && code.Length == CodeLength
            && code.All(char.IsDigit);
    }

    public static bool IsValidPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        return phoneNumber.Length <= MaxPhoneLength
            && phoneNumber[0] == '+'
            && phoneNumber.Skip(1).All(char.IsDigit)
            && phoneNumber.Length is >= 11 and <= 16;
    }
}
