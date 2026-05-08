using Microsoft.Extensions.Options;
using StampService.Application.Administration;
using StampService.Application.Brands.Commands.CreateBrandWithOwner;
using StampService.Application.Brands.Commands.ReassignBrandOwner;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Access;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Brands;

public class AdminBrandOwnerHandlerTests
{
    private const long AdminTelegramUserId = 278225388;

    [Fact]
    public async Task CreateBrandWithOwner_WhenAdminProvidesCustomerCode_ShouldCreateOwnerMembership()
    {
        var owner = User.Create("Owner", "1234").Value;
        var userRepository = new FakeUserRepository();
        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        userRepository.Add(owner);

        var handler = new CreateBrandWithOwnerHandler(
            CreateAdminAccessService(),
            brandRepository,
            membershipRepository,
            userRepository);

        var result = await handler.Handle(
            new CreateBrandWithOwnerCommand(AdminTelegramUserId, "Coffee", owner.CustomerCode),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(await brandRepository.ExistsAsync(result.Value.BrandId, CancellationToken.None));
        Assert.Equal(SystemRoles.Owner, await membershipRepository.GetRoleSystemNameAsync(
            owner.Id,
            result.Value.BrandId,
            CancellationToken.None));
    }

    [Fact]
    public async Task ReassignBrandOwner_WhenBrandHasOldOwner_ShouldRemoveOldMembershipAndAssignNewOwner()
    {
        var brandId = Guid.NewGuid();
        var oldOwner = User.Create("Old owner", "1111").Value;
        var newOwner = User.Create("New owner", "2222").Value;
        var userRepository = new FakeUserRepository();
        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();

        userRepository.Add(oldOwner);
        userRepository.Add(newOwner);
        brandRepository.AddExisting(brandId);
        membershipRepository.SetRole(oldOwner.Id, brandId, SystemRoles.Owner);
        membershipRepository.SetRole(newOwner.Id, brandId, SystemRoles.Staff);

        var handler = new ReassignBrandOwnerHandler(
            CreateAdminAccessService(),
            brandRepository,
            membershipRepository,
            userRepository);

        var result = await handler.Handle(
            new ReassignBrandOwnerCommand(AdminTelegramUserId, brandId, newOwner.CustomerCode),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(oldOwner.Id, result.Value.RemovedOwnerUserId);
        Assert.Null(await membershipRepository.GetRoleSystemNameAsync(
            oldOwner.Id,
            brandId,
            CancellationToken.None));
        Assert.Equal(SystemRoles.Owner, await membershipRepository.GetRoleSystemNameAsync(
            newOwner.Id,
            brandId,
            CancellationToken.None));
    }

    private static AdminAccessService CreateAdminAccessService()
    {
        return new AdminAccessService(Options.Create(new AdminOptions
        {
            TelegramUserIds = [AdminTelegramUserId]
        }));
    }
}
