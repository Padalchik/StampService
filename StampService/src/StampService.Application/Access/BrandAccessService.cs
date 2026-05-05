using StampService.Domain.Access;

namespace StampService.Application.Access;

public class BrandAccessService : IBrandAccessService
{
    private readonly IBrandMembershipRepository _brandMembershipRepository;

    public BrandAccessService(IBrandMembershipRepository brandMembershipRepository)
    {
        _brandMembershipRepository = brandMembershipRepository;
    }

    public async Task<bool> CanAsync(
        Guid userId,
        Guid brandId,
        PermissionCode permission,
        CancellationToken cancellationToken)
    {
        var role = await _brandMembershipRepository.GetRoleSystemNameAsync(
            userId,
            brandId,
            cancellationToken);

        return role switch
        {
            SystemRoles.Owner => true,
            SystemRoles.Staff => CanStaff(permission),
            SystemRoles.Customer => CanCustomer(permission),
            _ => false
        };
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
