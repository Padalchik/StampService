using Microsoft.Extensions.Options;
using StampService.Application.Administration;
using StampService.Application.Demo.Commands.CreateDemoBrands;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Demo;

public class CreateDemoBrandsHandlerTests
{
    [Fact]
    public async Task CreateDemoBrands_ShouldStageAllEntitiesAndSaveOnce()
    {
        var adminPhoneNumber = "+79214408362";
        var admin = User.Create("Admin").Value;
        admin.AddIdentity(IdentityType.Phone, adminPhoneNumber, "{}");
        var userRepository = new FakeUserRepository();
        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        var metricRepository = new FakeLoyaltyMetricRepository();
        var productRepository = new FakeCoinProductRepository();
        userRepository.Add(admin);

        var handler = new CreateDemoBrandsHandler(
            new AdminAccessService(Options.Create(new AdminOptions
            {
                PhoneNumbers = [adminPhoneNumber]
            }), userRepository),
            membershipRepository,
            brandRepository,
            productRepository,
            metricRepository,
            userRepository);

        var result = await handler.Handle(
            new CreateDemoBrandsCommand(AdminActor.FromUser(admin.Id)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, brandRepository.SaveCount);
        Assert.Equal(0, membershipRepository.SaveCount);
        Assert.Equal(0, metricRepository.SaveCount);
        Assert.Equal(0, productRepository.SaveCount);

        var brands = await brandRepository.GetAdminBrandsAsync(CancellationToken.None);
        Assert.Equal(5, brands.Count);

        var memberships = await membershipRepository.GetUserMembershipsAsync(admin.Id, CancellationToken.None);
        Assert.Equal(5, memberships.Count);
    }
}
