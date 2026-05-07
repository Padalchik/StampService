using StampService.Application.Access;
using StampService.Application.Brands.Queries.GetBrandWorkspace;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Access;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Brands;

public class GetBrandWorkspaceHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserIsOwner_ShouldAllowManagementActions()
    {
        var user = User.Create("user").Value;
        var brandId = Guid.NewGuid();
        var userRepository = new FakeUserRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        userRepository.Add(user);
        membershipRepository.SetRole(user.Id, brandId, SystemRoles.Owner, "Coffee");
        var handler = new GetBrandWorkspaceHandler(
            new BrandAccessService(membershipRepository),
            membershipRepository,
            userRepository);

        var result = await handler.Handle(
            new GetBrandWorkspaceQuery(user.Id, brandId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.CanIssue);
        Assert.True(result.Value.CanRedeem);
        Assert.True(result.Value.CanViewBalances);
        Assert.True(result.Value.CanManageMetrics);
        Assert.True(result.Value.CanManageStaff);
    }

    [Fact]
    public async Task Handle_WhenUserIsStaff_ShouldAllowOperationalActionsOnly()
    {
        var user = User.Create("user").Value;
        var brandId = Guid.NewGuid();
        var userRepository = new FakeUserRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        userRepository.Add(user);
        membershipRepository.SetRole(user.Id, brandId, SystemRoles.Staff, "Coffee");
        var handler = new GetBrandWorkspaceHandler(
            new BrandAccessService(membershipRepository),
            membershipRepository,
            userRepository);

        var result = await handler.Handle(
            new GetBrandWorkspaceQuery(user.Id, brandId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.CanIssue);
        Assert.True(result.Value.CanRedeem);
        Assert.True(result.Value.CanViewBalances);
        Assert.False(result.Value.CanManageMetrics);
        Assert.False(result.Value.CanManageStaff);
    }

    [Fact]
    public async Task Handle_WhenMembershipDoesNotExist_ShouldFail()
    {
        var user = User.Create("user").Value;
        var userRepository = new FakeUserRepository();
        userRepository.Add(user);
        var membershipRepository = new FakeBrandMembershipRepository();
        var handler = new GetBrandWorkspaceHandler(
            new BrandAccessService(membershipRepository),
            membershipRepository,
            userRepository);

        var result = await handler.Handle(
            new GetBrandWorkspaceQuery(user.Id, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }
}
