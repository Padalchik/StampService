using Microsoft.Extensions.Options;
using StampService.Application.Access;
using StampService.Application.Administration;
using StampService.Application.Brands.Commands.CreateBrandWithOwner;
using StampService.Application.Brands.Commands.ReassignBrandOwner;
using StampService.Application.Brands.Commands.UpdateBrandRewardSettings;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Access;
using StampService.Domain.Brand;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Brands;

public class AdminBrandOwnerHandlerTests
{
    private const long AdminTelegramUserId = 278225388;

    [Fact]
    public async Task CreateBrandWithOwner_WhenAdminProvidesPhone_ShouldCreateOwnerMembership()
    {
        var owner = User.Create("Owner", "1234").Value;
        var ownerPhoneNumber = "+79991234567";
        owner.AddIdentity(IdentityType.Phone, ownerPhoneNumber, "{}");
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
            new CreateBrandWithOwnerCommand(AdminTelegramUserId, "Coffee", ownerPhoneNumber),
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
        var newOwnerPhoneNumber = "+79997654321";
        newOwner.AddIdentity(IdentityType.Phone, newOwnerPhoneNumber, "{}");
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
            new ReassignBrandOwnerCommand(AdminTelegramUserId, brandId, newOwnerPhoneNumber),
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

    [Fact]
    public async Task UpdateBrandRewardSettings_WhenActorIsOwner_ShouldUpdateSettings()
    {
        var owner = User.Create("Owner", "1234").Value;
        var brand = Brand.Create("Coffee").Value;
        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        brandRepository.AddExisting(brand);
        membershipRepository.SetRole(owner.Id, brand.Id, SystemRoles.Owner);
        var handler = new UpdateBrandRewardSettingsHandler(
            new BrandAccessService(membershipRepository),
            brandRepository);

        var result = await handler.Handle(
            new UpdateBrandRewardSettingsCommand(
                owner.Id,
                brand.Id,
                IsMetricsEnabled: false,
                IsCoinsEnabled: true,
                IsCoinProductRedemptionEnabled: true,
                IsManualCoinRedemptionEnabled: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Coffee", brand.Name);
        Assert.False(brand.IsMetricsEnabled);
        Assert.True(brand.IsCoinsEnabled);
        Assert.True(brand.IsCoinProductRedemptionEnabled);
        Assert.True(brand.IsManualCoinRedemptionEnabled);
    }

    [Fact]
    public async Task UpdateBrandRewardSettings_WhenActorIsStaff_ShouldFail()
    {
        var staff = User.Create("Staff", "1234").Value;
        var brand = Brand.Create("Coffee").Value;
        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        brandRepository.AddExisting(brand);
        membershipRepository.SetRole(staff.Id, brand.Id, SystemRoles.Staff);
        var handler = new UpdateBrandRewardSettingsHandler(
            new BrandAccessService(membershipRepository),
            brandRepository);

        var result = await handler.Handle(
            new UpdateBrandRewardSettingsCommand(
                staff.Id,
                brand.Id,
                IsMetricsEnabled: false,
                IsCoinsEnabled: true,
                IsCoinProductRedemptionEnabled: true,
                IsManualCoinRedemptionEnabled: false),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.True(brand.IsMetricsEnabled);
    }

    [Fact]
    public async Task UpdateBrandRewardSettings_WhenCoinsEnabledWithoutRedemptionModes_ShouldFailWithoutChangingBrand()
    {
        var owner = User.Create("Owner", "1234").Value;
        var brand = Brand.Create("Coffee").Value;
        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        brandRepository.AddExisting(brand);
        membershipRepository.SetRole(owner.Id, brand.Id, SystemRoles.Owner);
        var handler = new UpdateBrandRewardSettingsHandler(
            new BrandAccessService(membershipRepository),
            brandRepository);

        var result = await handler.Handle(
            new UpdateBrandRewardSettingsCommand(
                owner.Id,
                brand.Id,
                IsMetricsEnabled: true,
                IsCoinsEnabled: true,
                IsCoinProductRedemptionEnabled: false,
                IsManualCoinRedemptionEnabled: false),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.True(brand.IsMetricsEnabled);
        Assert.True(brand.IsCoinsEnabled);
        Assert.True(brand.IsCoinProductRedemptionEnabled);
        Assert.False(brand.IsManualCoinRedemptionEnabled);
    }

    private static AdminAccessService CreateAdminAccessService()
    {
        return new AdminAccessService(Options.Create(new AdminOptions
        {
            TelegramUserIds = [AdminTelegramUserId]
        }));
    }
}
