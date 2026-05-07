using StampService.Application.Brands.Queries.GetMyBrands;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Access;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Brands;

public class GetMyBrandsHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserHasMemberships_ShouldReturnBrands()
    {
        var user = User.Create("user").Value;
        var userRepository = new FakeUserRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        userRepository.Add(user);
        membershipRepository.SetRole(user.Id, Guid.NewGuid(), SystemRoles.Owner, "Coffee");
        membershipRepository.SetRole(user.Id, Guid.NewGuid(), SystemRoles.Staff, "Bakery");
        var handler = new GetMyBrandsHandler(membershipRepository, userRepository);

        var result = await handler.Handle(new GetMyBrandsQuery(user.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Brands.Count);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoMemberships_ShouldReturnEmptyList()
    {
        var user = User.Create("user").Value;
        var userRepository = new FakeUserRepository();
        userRepository.Add(user);
        var handler = new GetMyBrandsHandler(
            new FakeBrandMembershipRepository(),
            userRepository);

        var result = await handler.Handle(new GetMyBrandsQuery(user.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Brands);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldFail()
    {
        var handler = new GetMyBrandsHandler(
            new FakeBrandMembershipRepository(),
            new FakeUserRepository());

        var result = await handler.Handle(new GetMyBrandsQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task Handle_WhenUserIdIsEmpty_ShouldFail()
    {
        var handler = new GetMyBrandsHandler(
            new FakeBrandMembershipRepository(),
            new FakeUserRepository());

        var result = await handler.Handle(new GetMyBrandsQuery(Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailed);
    }
}
