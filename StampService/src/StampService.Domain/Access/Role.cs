using FluentResults;
using StampService.Domain.Shared;

namespace StampService.Domain.Access;

public class Role
{
    public Guid Id { get; private set; }
    public string SystemName { get; private set; }
    public string DisplayName { get; private set; }

    private Role(string systemName, string displayName)
    {
        Id = Guid.NewGuid();
        SystemName = systemName;
        DisplayName = displayName;
    }

    // EF Core
    protected Role()
    {
        Id = Guid.Empty;
        SystemName = null!;
        DisplayName = null!;
    }

    public static Result<Role> Create(string systemName, string displayName)
    {
        if (string.IsNullOrWhiteSpace(systemName))
            return Result.Fail(DomainError.Validation(
                "role.system_name_required",
                "SystemName не может быть пустым",
                nameof(systemName)));

        if (systemName.Length > Constants.MAX_ROLE_SYSTEM_NAME_LENGTH)
            return Result.Fail(DomainError.Validation(
                "role.system_name_too_long",
                $"SystemName не должен превышать {Constants.MAX_ROLE_SYSTEM_NAME_LENGTH} символов",
                nameof(systemName)));

        if (string.IsNullOrWhiteSpace(displayName))
            return Result.Fail(DomainError.Validation(
                "role.display_name_required",
                "DisplayName не может быть пустым",
                nameof(displayName)));

        if (displayName.Length > Constants.MAX_ROLE_DISPLAY_NAME_LENGTH)
            return Result.Fail(DomainError.Validation(
                "role.display_name_too_long",
                $"DisplayName не должен превышать {Constants.MAX_ROLE_DISPLAY_NAME_LENGTH} символов",
                nameof(displayName)));

        var role = new Role(systemName.Trim(), displayName.Trim());
        return Result.Ok(role);
    }
}
