using StampService.Application.Access;
using StampService.Domain.Access;

namespace StampService.ApplicationTests.Fakes;

public class FakeBrandMembershipRepository : IBrandMembershipRepository
{
    private readonly Dictionary<(Guid UserId, Guid BrandId), string> _roleSystemNames = [];

    public void SetRole(Guid userId, Guid brandId, string roleSystemName)
    {
        _roleSystemNames[(userId, brandId)] = roleSystemName;
    }

    public Task<string?> GetRoleSystemNameAsync(
        Guid userId,
        Guid brandId,
        CancellationToken cancellationToken)
    {
        _roleSystemNames.TryGetValue((userId, brandId), out var roleSystemName);
        return Task.FromResult(roleSystemName);
    }

    public Task<BrandMembership?> GetByBrandAndUserAsync(
        Guid brandId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<BrandMembership?> GetOwnerAsync(Guid brandId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<Role?> GetRoleBySystemNameAsync(string systemName, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public void Add(BrandMembership membership)
    {
        throw new NotSupportedException();
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }
}
