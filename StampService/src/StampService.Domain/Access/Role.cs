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
    }

    public static Result<Role> Create(string systemName, string displayName)
    {
        if (string.IsNullOrWhiteSpace(systemName))
            return Result.Fail("SystemName не может быть пустым");

        if (string.IsNullOrWhiteSpace(displayName))
            return Result.Fail("DisplayName не может быть пустым");

        var role = new Role(systemName.Trim(), displayName.Trim());
        return Result.Ok(role);
    }
}
