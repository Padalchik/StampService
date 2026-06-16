using Microsoft.Extensions.Options;
using StampService.Application.Access;
using StampService.Application.Administration;
using StampService.Application.Brands.Commands.CreateBrandWithOwner;
using StampService.Application.Brands.Commands.ReassignBrandOwner;
using StampService.Application.Brands.Commands.UpdateBrandRewardSettings;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Access;
using StampService.Domain.Brand;
using StampService.Domain.Loyalty;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Brands;

public class AdminBrandOwnerHandlerTests
{
    private const long AdminTelegramUserId = 278225388;

    [Fact]
    public async Task CreateBrandWithOwner_WhenAdminProvidesPhone_ShouldCreateOwnerMembership()
    {
        var owner = User.Create("Owner").Value;
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
            new CreateBrandWithOwnerCommand(AdminActor.FromTelegram(AdminTelegramUserId), "Coffee", ownerPhoneNumber),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, brandRepository.SaveCount);
        Assert.Equal(1, membershipRepository.SaveCount);
        Assert.True(await brandRepository.ExistsAsync(result.Value.BrandId, CancellationToken.None));
        Assert.Equal(SystemRoles.Owner, await membershipRepository.GetRoleSystemNameAsync(
            owner.Id,
            result.Value.BrandId,
            CancellationToken.None));
    }

    [Fact]
    public async Task CreateBrandWithOwner_WhenWebAdminPhoneIsConfigured_ShouldCreateOwnerMembership()
    {
        var admin = User.Create("Admin").Value;
        admin.AddIdentity(IdentityType.Phone, "+79214408362", "{}");
        var owner = User.Create("Owner").Value;
        var ownerPhoneNumber = "+79991234567";
        owner.AddIdentity(IdentityType.Phone, ownerPhoneNumber, "{}");
        var userRepository = new FakeUserRepository();
        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        userRepository.Add(admin);
        userRepository.Add(owner);

        var handler = new CreateBrandWithOwnerHandler(
            new AdminAccessService(Options.Create(new AdminOptions
            {
                PhoneNumbers = ["+79214408362"]
            }), userRepository),
            brandRepository,
            membershipRepository,
            userRepository);

        var result = await handler.Handle(
            new CreateBrandWithOwnerCommand(AdminActor.FromUser(admin.Id), "Coffee", ownerPhoneNumber),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(SystemRoles.Owner, await membershipRepository.GetRoleSystemNameAsync(
            owner.Id,
            result.Value.BrandId,
            CancellationToken.None));
    }

    [Fact]
    public async Task ReassignBrandOwner_WhenBrandHasOldOwner_ShouldRemoveOldMembershipAndAssignNewOwner()
    {
        var brandId = Guid.NewGuid();
        var oldOwner = User.Create("Old owner").Value;
        var newOwner = User.Create("New owner").Value;
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
            new ReassignBrandOwnerCommand(AdminActor.FromTelegram(AdminTelegramUserId), brandId, newOwnerPhoneNumber),
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
        var owner = User.Create("Owner").Value;
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
        var staff = User.Create("Staff").Value;
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
        var owner = User.Create("Owner").Value;
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

    [Fact]
    public async Task UpdateBrandRewardSettings_WhenWelcomeRewardsAreConfigured_ShouldSaveWelcomeSettings()
    {
        var owner = User.Create("Owner").Value;
        var brand = Brand.Create("Coffee").Value;
        var metric = LoyaltyMetricDefinition.Create(brand.Id, "Visit", redemptionAmount: 5).Value;
        var brandRepository = new FakeBrandRepository();
        var metricRepository = new FakeLoyaltyMetricRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        brandRepository.AddExisting(brand);
        metricRepository.AddExisting(metric);
        membershipRepository.SetRole(owner.Id, brand.Id, SystemRoles.Owner);
        var handler = new UpdateBrandRewardSettingsHandler(
            new BrandAccessService(membershipRepository),
            brandRepository,
            metricRepository);

        var result = await handler.Handle(
            new UpdateBrandRewardSettingsCommand(
                owner.Id,
                brand.Id,
                IsMetricsEnabled: true,
                IsCoinsEnabled: true,
                IsCoinProductRedemptionEnabled: true,
                IsManualCoinRedemptionEnabled: false,
                WelcomeMetrics: [new BrandWelcomeMetricRewardSetting(metric.Id, 2)],
                WelcomeCoinsAmount: 7,
                WelcomeRewardComment: "Приветственная награда",
                IsWelcomeRewardsEnabled: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(brand.IsWelcomeRewardsEnabled);
        Assert.Single(brand.WelcomeMetricRewards);
        Assert.Equal(metric.Id, brand.WelcomeMetricRewards.Single().MetricDefinitionId);
        Assert.Equal(2, brand.WelcomeMetricRewards.Single().Amount);
        Assert.Equal(7, brand.WelcomeCoinsAmount);
        Assert.Equal("Приветственная награда", brand.WelcomeRewardComment);
        Assert.True(result.Value.WelcomeRewards.IsEnabled);
    }

    [Fact]
    public async Task UpdateBrandRewardSettings_WhenWelcomeMetricDoesNotBelongToBrand_ShouldFail()
    {
        var owner = User.Create("Owner").Value;
        var brand = Brand.Create("Coffee").Value;
        var otherBrandMetric = LoyaltyMetricDefinition.Create(Guid.NewGuid(), "Visit", redemptionAmount: 5).Value;
        var brandRepository = new FakeBrandRepository();
        var metricRepository = new FakeLoyaltyMetricRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        brandRepository.AddExisting(brand);
        metricRepository.AddExisting(otherBrandMetric);
        membershipRepository.SetRole(owner.Id, brand.Id, SystemRoles.Owner);
        var handler = new UpdateBrandRewardSettingsHandler(
            new BrandAccessService(membershipRepository),
            brandRepository,
            metricRepository);

        var result = await handler.Handle(
            new UpdateBrandRewardSettingsCommand(
                owner.Id,
                brand.Id,
                IsMetricsEnabled: true,
                IsCoinsEnabled: true,
                IsCoinProductRedemptionEnabled: true,
                IsManualCoinRedemptionEnabled: false,
                WelcomeMetrics: [new BrandWelcomeMetricRewardSetting(otherBrandMetric.Id, 1)],
                WelcomeCoinsAmount: 0,
                IsWelcomeRewardsEnabled: true),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.False(brand.IsWelcomeRewardsEnabled);
        Assert.Empty(brand.WelcomeMetricRewards);
    }

    private static AdminAccessService CreateAdminAccessService(FakeUserRepository? userRepository = null)
    {
        return new AdminAccessService(Options.Create(new AdminOptions
        {
            TelegramUserIds = [AdminTelegramUserId]
        }), userRepository ?? new FakeUserRepository());
    }
}
