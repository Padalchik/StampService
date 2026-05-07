using StampService.Domain.Access;

namespace StampService.DomainTests.Access;

public class BrandMembershipTests
{
    [Fact]
    public void Create_ValidData_ShouldCreateMembership()
    {
        var userId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var result = BrandMembership.Create(userId, brandId, roleId);

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value.UserId);
        Assert.Equal(brandId, result.Value.BrandId);
        Assert.Equal(roleId, result.Value.RoleId);
    }

    [Fact]
    public void Create_EmptyUserId_ShouldFail()
    {
        var result = BrandMembership.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());

        Assert.True(result.IsFailed);
    }

    [Fact]
    public void ChangeRole_ValidRoleId_ShouldChangeRole()
    {
        var membership = BrandMembership.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Value;
        var newRoleId = Guid.NewGuid();

        var result = membership.ChangeRole(newRoleId);

        Assert.True(result.IsSuccess);
        Assert.Equal(newRoleId, membership.RoleId);
    }

    [Fact]
    public void ChangeRole_EmptyRoleId_ShouldFailAndKeepCurrentRole()
    {
        var roleId = Guid.NewGuid();
        var membership = BrandMembership.Create(Guid.NewGuid(), Guid.NewGuid(), roleId).Value;

        var result = membership.ChangeRole(Guid.Empty);

        Assert.True(result.IsFailed);
        Assert.Equal(roleId, membership.RoleId);
    }
}
