using StampService.Domain.Access;

namespace StampService.Application.Access;

public interface IBrandAccessService
{
    Task<bool> CanAsync(
        Guid userId,
        Guid brandId,
        PermissionCode permission,
        CancellationToken cancellationToken);
}
