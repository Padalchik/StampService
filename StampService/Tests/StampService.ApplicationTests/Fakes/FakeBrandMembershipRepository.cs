using StampService.Application.Access;
using StampService.Application.Brands;
using StampService.Domain.Access;

namespace StampService.ApplicationTests.Fakes;

public class FakeBrandMembershipRepository : IBrandMembershipRepository
{
    private readonly Dictionary<(Guid UserId, Guid BrandId), string> _roleSystemNames = [];
    private readonly Dictionary<Guid, string> _brandNames = [];

    public void SetRole(Guid userId, Guid brandId, string roleSystemName, string? brandName = null)
    {
        _roleSystemNames[(userId, brandId)] = roleSystemName;
        _brandNames[brandId] = brandName ?? $"Brand {brandId:N}";
    }

    public Task<string?> GetRoleSystemNameAsync(
        Guid userId,
        Guid brandId,
        CancellationToken cancellationToken)
    {
        _roleSystemNames.TryGetValue((userId, brandId), out var roleSystemName);
        return Task.FromResult(roleSystemName);
    }

    public Task<IReadOnlyCollection<UserBrandMembershipReadModel>> GetUserBrandMembershipsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<UserBrandMembershipReadModel> result = _roleSystemNames
            .Where(item => item.Key.UserId == userId)
            .Select(item => new UserBrandMembershipReadModel(
                item.Key.BrandId,
                _brandNames[item.Key.BrandId],
                item.Value))
            .OrderBy(item => item.BrandName)
            .ToArray();

        return Task.FromResult(result);
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
