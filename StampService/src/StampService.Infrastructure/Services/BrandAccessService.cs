using Microsoft.EntityFrameworkCore;
using StampService.Application.Access;
using StampService.Domain.Access;

namespace StampService.Infrastructure.Services;

public class BrandAccessService : IBrandAccessService
{
    private readonly AppDbContext _dbContext;

    public BrandAccessService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> CanAsync(
        Guid userId,
        Guid brandId,
        PermissionCode permission,
        CancellationToken cancellationToken)
    {
        var role = await GetUserRoleAsync(userId, brandId, cancellationToken);

        return role switch
        {
            SystemRoles.Owner => true,
            SystemRoles.Staff => CanStaff(permission),
            SystemRoles.Customer => CanCustomer(permission),
            _ => false
        };
    }

    private async Task<string?> GetUserRoleAsync(
        Guid userId,
        Guid brandId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.BrandMemberships
            .AsNoTracking()
            .Where(membership => membership.UserId == userId && membership.BrandId == brandId)
            .Select(membership => membership.Role.SystemName)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static bool CanStaff(PermissionCode permission)
    {
        return permission is
            PermissionCode.StampIssue or
            PermissionCode.BalanceView;
    }

    private static bool CanCustomer(PermissionCode permission)
    {
        return permission is PermissionCode.BalanceView;
    }
}
