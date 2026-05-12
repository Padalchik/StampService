using FluentResults;
using StampService.Domain.Shared;

namespace StampService.Domain.User;

public class RedemptionCode : BaseEntity
{
    public const int CodeLength = 4;

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string Code { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }

    private RedemptionCode(Guid userId, string code, DateTime expiresAtUtc)
    {
        UserId = userId;
        Code = code;
        ExpiresAtUtc = expiresAtUtc;
    }

    // EF Core
    protected RedemptionCode()
    {
        Code = null!;
    }

    public static Result<RedemptionCode> Create(
        Guid userId,
        string code,
        DateTime expiresAtUtc,
        DateTime nowUtc)
    {
        if (userId == Guid.Empty)
            return Result.Fail(DomainError.Validation(
                "redemption_code.user_id_empty",
                "UserId cannot be empty GUID",
                nameof(userId)));

        if (!IsValidCode(code))
            return Result.Fail(DomainError.Validation(
                "redemption_code.code_invalid",
                $"Redemption code must contain exactly {CodeLength} digits",
                nameof(code)));

        if (expiresAtUtc <= nowUtc)
            return Result.Fail(DomainError.Validation(
                "redemption_code.expires_at_invalid",
                "Redemption code expiration date must be in the future",
                nameof(expiresAtUtc)));

        return Result.Ok(new RedemptionCode(userId, code, expiresAtUtc));
    }

    public Result Use(DateTime nowUtc)
    {
        if (UsedAtUtc is not null)
            return Result.Fail(DomainError.Conflict(
                "redemption_code.already_used",
                "Redemption code has already been used"));

        if (IsExpired(nowUtc))
            return Result.Fail(DomainError.Validation(
                "redemption_code.expired",
                "Redemption code has expired"));

        UsedAtUtc = nowUtc;
        Touch();

        return Result.Ok();
    }

    public bool IsActive(DateTime nowUtc)
    {
        return UsedAtUtc is null && !IsExpired(nowUtc);
    }

    public static bool IsValidCode(string? code)
    {
        return code is not null
            && code.Length == CodeLength
            && code.All(char.IsDigit);
    }

    private bool IsExpired(DateTime nowUtc)
    {
        return ExpiresAtUtc <= nowUtc;
    }
}
