using FluentResults;
using StampService.Domain.Shared;

namespace StampService.Domain.Access;

public class Role : BaseEntity
{
    public string SystemName { get; private set; }
    public string DisplayName { get; private set; }

    private Role(string systemName, string displayName)
    {
        SystemName = systemName;
        DisplayName = displayName;
    }

    // EF Core
    protected Role()
    {
        SystemName = null!;
        DisplayName = null!;
    }

    public static Result<Role> Create(string systemName, string displayName)
    {
        if (string.IsNullOrWhiteSpace(systemName))
            return Result.Fail("SystemName не может быть пустым");

        if (systemName.Length > Constants.MAX_ROLE_SYSTEM_NAME_LENGTH)
            return Result.Fail($"SystemName не должен превышать {Constants.MAX_ROLE_SYSTEM_NAME_LENGTH} символов");

        if (string.IsNullOrWhiteSpace(displayName))
            return Result.Fail("DisplayName не может быть пустым");

        if (displayName.Length > Constants.MAX_ROLE_DISPLAY_NAME_LENGTH)
            return Result.Fail($"DisplayName не должен превышать {Constants.MAX_ROLE_DISPLAY_NAME_LENGTH} символов");

        var role = new Role(systemName.Trim(), displayName.Trim());
        return Result.Ok(role);
    }
}
