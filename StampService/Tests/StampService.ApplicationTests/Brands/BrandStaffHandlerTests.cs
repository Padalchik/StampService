using StampService.Application.Access;
using StampService.Application.Brands.Commands.AddBrandStaffByCustomerCode;
using StampService.Application.Brands.Commands.RemoveBrandStaff;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Access;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Brands;

public class BrandStaffHandlerTests
{
    [Fact]
    public async Task AddStaffByCustomerCode_WhenActorIsOwner_ShouldCreateStaffMembership()
    {
        var brandId = Guid.NewGuid();
        var owner = User.Create("Owner", "1111").Value;
        var staff = User.Create("Staff", "1234").Value;
        var userRepository = new FakeUserRepository();
        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        userRepository.Add(owner);
        userRepository.Add(staff);
        brandRepository.AddExisting(brandId);
        membershipRepository.SetRole(owner.Id, brandId, SystemRoles.Owner);

        var handler = new AddBrandStaffByCustomerCodeHandler(
            new BrandAccessService(membershipRepository),
            new BrandMembershipService(brandRepository, membershipRepository, userRepository),
            userRepository);

        var result = await handler.Handle(
            new AddBrandStaffByCustomerCodeCommand(owner.Id, brandId, staff.CustomerCode),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(SystemRoles.Staff, await membershipRepository.GetRoleSystemNameAsync(
            staff.Id,
            brandId,
            CancellationToken.None));
    }

    [Fact]
    public async Task RemoveStaff_WhenActorIsOwner_ShouldRemoveStaffMembership()
    {
        var brandId = Guid.NewGuid();
        var owner = User.Create("Owner", "1111").Value;
        var staff = User.Create("Staff", "2222").Value;
        var userRepository = new FakeUserRepository();
        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        userRepository.Add(owner);
        userRepository.Add(staff);
        brandRepository.AddExisting(brandId);
        membershipRepository.SetRole(owner.Id, brandId, SystemRoles.Owner);
        membershipRepository.SetRole(staff.Id, brandId, SystemRoles.Staff);

        var handler = new RemoveBrandStaffHandler(
            new BrandAccessService(membershipRepository),
            membershipRepository,
            brandRepository,
            userRepository);

        var result = await handler.Handle(
            new RemoveBrandStaffCommand(owner.Id, brandId, staff.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(await membershipRepository.GetRoleSystemNameAsync(
            staff.Id,
            brandId,
            CancellationToken.None));
    }
}
