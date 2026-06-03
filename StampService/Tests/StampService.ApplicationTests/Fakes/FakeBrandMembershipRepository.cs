using StampService.Application.Access;
using StampService.Application.Brands;
using StampService.Domain.Access;

namespace StampService.ApplicationTests.Fakes;

public class FakeBrandMembershipRepository : IBrandMembershipRepository
{
    private readonly Dictionary<(Guid UserId, Guid BrandId), BrandMembership> _memberships = [];
    private readonly Dictionary<Guid, string> _brandNames = [];
    private readonly Dictionary<string, Role> _rolesBySystemName = new(StringComparer.Ordinal)
    {
        [SystemRoles.Owner] = Role.Create(SystemRoles.Owner, "Owner").Value,
        [SystemRoles.Staff] = Role.Create(SystemRoles.Staff, "Staff").Value
    };

    public int SaveCount { get; private set; }

    public void SetRole(Guid userId, Guid brandId, string roleSystemName, string? brandName = null)
    {
        var role = GetKnownRole(roleSystemName);
        _memberships[(userId, brandId)] = BrandMembership.Create(userId, brandId, role.Id).Value;
        _brandNames[brandId] = brandName ?? $"Brand {brandId:N}";
    }

    public Task<string?> GetRoleSystemNameAsync(
        Guid userId,
        Guid brandId,
        CancellationToken cancellationToken)
    {
        var membership = _memberships.Values.FirstOrDefault(item =>
            item.UserId == userId && item.BrandId == brandId);
        var roleSystemName = membership is null
            ? null
            : GetRoleSystemName(membership.RoleId);

        return Task.FromResult(roleSystemName);
    }

    public Task<IReadOnlyCollection<UserBrandMembershipReadModel>> GetUserBrandMembershipsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<UserBrandMembershipReadModel> result = _memberships
            .Where(item => item.Value.UserId == userId)
            .Select(item => new UserBrandMembershipReadModel(
                item.Value.BrandId,
                _brandNames[item.Value.BrandId],
                GetRoleSystemName(item.Value.RoleId)))
            .OrderBy(item => item.BrandName)
            .ToArray();

        return Task.FromResult(result);
    }

    public Task<IReadOnlyCollection<BrandMembership>> GetUserMembershipsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<BrandMembership> result = _memberships.Values
            .Where(membership => membership.UserId == userId)
            .ToArray();

        return Task.FromResult(result);
    }

    public Task<IReadOnlyCollection<BrandStaffReadModel>> GetBrandStaffAsync(
        Guid brandId,
        CancellationToken cancellationToken)
    {
        var staffRole = GetKnownRole(SystemRoles.Staff);
        IReadOnlyCollection<BrandStaffReadModel> result = _memberships.Values
            .Where(membership => membership.BrandId == brandId && membership.RoleId == staffRole.Id)
            .Select(membership => new BrandStaffReadModel(
                membership.UserId,
                $"User {membership.UserId:N}",
                "0000",
                membership.CreatedAt))
            .ToArray();

        return Task.FromResult(result);
    }

    public Task<BrandMembership?> GetByBrandAndUserAsync(
        Guid brandId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var membership = _memberships.Values.FirstOrDefault(item =>
            item.UserId == userId && item.BrandId == brandId);
        return Task.FromResult(membership);
    }

    public Task<BrandMembership?> GetOwnerAsync(Guid brandId, CancellationToken cancellationToken)
    {
        var ownerRole = GetKnownRole(SystemRoles.Owner);
        var owner = _memberships.Values.FirstOrDefault(membership =>
            membership.BrandId == brandId && membership.RoleId == ownerRole.Id);

        return Task.FromResult(owner);
    }

    public Task<Role?> GetRoleBySystemNameAsync(string systemName, CancellationToken cancellationToken)
    {
        _rolesBySystemName.TryGetValue(systemName, out var role);
        return Task.FromResult(role);
    }

    public void Add(BrandMembership membership)
    {
        _ = GetRoleSystemName(membership.RoleId);
        _memberships[(membership.UserId, membership.BrandId)] = membership;
        _brandNames.TryAdd(membership.BrandId, $"Brand {membership.BrandId:N}");
    }

    public void Remove(BrandMembership membership)
    {
        _memberships.Remove((membership.UserId, membership.BrandId));
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    private Role GetKnownRole(string roleSystemName)
    {
        return _rolesBySystemName.TryGetValue(roleSystemName, out var role)
            ? role
            : throw new InvalidOperationException($"Unknown role '{roleSystemName}'.");
    }

    private string GetRoleSystemName(Guid roleId)
    {
        var role = _rolesBySystemName.Values.FirstOrDefault(item => item.Id == roleId);

        return role?.SystemName
            ?? throw new InvalidOperationException($"Unknown role id '{roleId}'.");
    }
}
