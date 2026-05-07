using StampService.Domain.Access;
using StampService.Application.Brands;

namespace StampService.Application.Access;

public interface IBrandMembershipRepository
{
    Task<string?> GetRoleSystemNameAsync(
        Guid userId,
        Guid brandId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UserBrandMembershipReadModel>> GetUserBrandMembershipsAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<BrandMembership?> GetByBrandAndUserAsync(
        Guid brandId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<BrandMembership?> GetOwnerAsync(Guid brandId, CancellationToken cancellationToken);

    Task<Role?> GetRoleBySystemNameAsync(string systemName, CancellationToken cancellationToken);

    void Add(BrandMembership membership);

    Task SaveAsync(CancellationToken cancellationToken);
}
