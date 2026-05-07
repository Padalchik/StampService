using StampService.Application.Access;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Access;

namespace StampService.ApplicationTests.Access;

public class BrandAccessServiceTests
{
    [Fact]
    public async Task CanAsync_Owner_ShouldAllowEveryPermission()
    {
        var userId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var repository = new FakeBrandMembershipRepository();
        repository.SetRole(userId, brandId, SystemRoles.Owner);
        var service = new BrandAccessService(repository);

        foreach (var permission in Enum.GetValues<PermissionCode>())
        {
            var can = await service.CanAsync(userId, brandId, permission, CancellationToken.None);

            Assert.True(can);
        }
    }

    [Fact]
    public async Task CanAsync_Staff_ShouldAllowOperationalPermissions()
    {
        var userId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var service = CreateServiceWithRole(userId, brandId, SystemRoles.Staff);

        Assert.True(await service.CanAsync(userId, brandId, PermissionCode.StampIssue, CancellationToken.None));
        Assert.True(await service.CanAsync(userId, brandId, PermissionCode.StampRedeem, CancellationToken.None));
        Assert.True(await service.CanAsync(userId, brandId, PermissionCode.BalanceView, CancellationToken.None));
    }

    [Fact]
    public async Task CanAsync_Staff_ShouldDenyManagementPermissions()
    {
        var userId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var service = CreateServiceWithRole(userId, brandId, SystemRoles.Staff);

        Assert.False(await service.CanAsync(userId, brandId, PermissionCode.MetricManage, CancellationToken.None));
        Assert.False(await service.CanAsync(userId, brandId, PermissionCode.StaffManage, CancellationToken.None));
        Assert.False(await service.CanAsync(userId, brandId, PermissionCode.BrandManage, CancellationToken.None));
    }

    [Fact]
    public async Task CanAsync_NoMembership_ShouldDenyPermission()
    {
        var service = new BrandAccessService(new FakeBrandMembershipRepository());

        var can = await service.CanAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PermissionCode.StampIssue,
            CancellationToken.None);

        Assert.False(can);
    }

    private static BrandAccessService CreateServiceWithRole(
        Guid userId,
        Guid brandId,
        string roleSystemName)
    {
        var repository = new FakeBrandMembershipRepository();
        repository.SetRole(userId, brandId, roleSystemName);
        return new BrandAccessService(repository);
    }
}
