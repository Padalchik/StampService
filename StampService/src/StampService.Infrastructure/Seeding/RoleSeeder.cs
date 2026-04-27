using Microsoft.EntityFrameworkCore;
using StampService.Domain.Access;

namespace StampService.Infrastructure.Seeding;

public static class RoleSeeder
{
    public static async Task SeedSystemRolesAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var existingSystemNames = await dbContext.Roles
            .AsNoTracking()
            .Select(x => x.SystemName)
            .ToHashSetAsync(cancellationToken);

        var rolesToSeed = new (string SystemName, string DisplayName)[]
        {
            (SystemRoles.Owner, "Owner"),
            (SystemRoles.Staff, "Staff"),
            (SystemRoles.Customer, "Customer")
        };

        foreach (var roleDefinition in rolesToSeed)
        {
            if (existingSystemNames.Contains(roleDefinition.SystemName))
                continue;

            var roleResult = Role.Create(roleDefinition.SystemName, roleDefinition.DisplayName);
            if (roleResult.IsFailed)
            {
                var errorMessage = string.Join("; ", roleResult.Errors.Select(x => x.Message));
                throw new InvalidOperationException(
                    $"Failed to seed role '{roleDefinition.SystemName}': {errorMessage}");
            }

            dbContext.Roles.Add(roleResult.Value);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
