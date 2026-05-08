using StampService.Application.Access;
using StampService.Domain.Access;

namespace StampService.Application.Brands;

internal static class BrandStaffAuthorization
{
    public static async Task<bool> CanManageStaffAsync(
        IBrandAccessService brandAccessService,
        Guid actorUserId,
        Guid brandId,
        CancellationToken cancellationToken)
    {
        return actorUserId != Guid.Empty
            && await brandAccessService.CanAsync(
                actorUserId,
                brandId,
                PermissionCode.StaffManage,
                cancellationToken);
    }
}
