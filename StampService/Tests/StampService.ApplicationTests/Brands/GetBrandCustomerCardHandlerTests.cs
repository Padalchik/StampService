using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Brands.Queries.GetBrandCustomerCard;
using StampService.Application.Wallet.Queries.GetUserWalletBrandDetails;
using StampService.ApplicationTests.Fakes;
using StampService.Contracts.DTOs.Wallet;
using StampService.Domain.Access;
using StampService.Domain.Brand;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Brands;

public class GetBrandCustomerCardHandlerTests
{
    [Fact]
    public async Task Handle_WhenPhoneUserExistsButIsNotBrandCustomer_ShouldFail()
    {
        var brandId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var phoneNumber = "+79991234567";
        var customer = CreatePhoneUser(phoneNumber);
        var userRepository = new FakeUserRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        var brandCustomerRepository = new FakeBrandCustomerRepository(userRepository);
        var walletHandler = new StubWalletDetailsHandler();
        userRepository.Add(customer);
        membershipRepository.SetRole(actorUserId, brandId, SystemRoles.Staff);

        var handler = new GetBrandCustomerCardHandler(
            new BrandAccessService(membershipRepository),
            brandCustomerRepository,
            walletHandler);

        var result = await handler.Handle(
            new GetBrandCustomerCardQuery(actorUserId, brandId, phoneNumber),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(0, walletHandler.CallCount);
    }

    [Fact]
    public async Task Handle_WhenPhoneUserIsBrandCustomer_ShouldReturnCard()
    {
        var brandId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var phoneNumber = "+79991234567";
        var customer = CreatePhoneUser(phoneNumber);
        var userRepository = new FakeUserRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        var brandCustomerRepository = new FakeBrandCustomerRepository(userRepository);
        var walletHandler = new StubWalletDetailsHandler();
        userRepository.Add(customer);
        brandCustomerRepository.AddExisting(brandId, customer.Id, actorUserId);
        membershipRepository.SetRole(actorUserId, brandId, SystemRoles.Staff);

        var handler = new GetBrandCustomerCardHandler(
            new BrandAccessService(membershipRepository),
            brandCustomerRepository,
            walletHandler);

        var result = await handler.Handle(
            new GetBrandCustomerCardQuery(actorUserId, brandId, phoneNumber),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(customer.Id, result.Value.CustomerUserId);
        Assert.Equal(1, walletHandler.CallCount);
    }

    private static User CreatePhoneUser(string phoneNumber)
    {
        var user = User.Create("Customer").Value;
        user.AddIdentity(IdentityType.Phone, phoneNumber, "{}");
        return user;
    }

    private sealed class StubWalletDetailsHandler
        : IQueryHandler<UserWalletBrandDetailsResponse, GetUserWalletBrandDetailsQuery>
    {
        public int CallCount { get; private set; }

        public Task<Result<UserWalletBrandDetailsResponse>> Handle(
            GetUserWalletBrandDetailsQuery query,
            CancellationToken cancellationToken)
        {
            CallCount++;

            var response = new UserWalletBrandDetailsResponse(
                query.UserId,
                query.BrandId,
                "Coffee",
                IsMetricsEnabled: true,
                IsCoinsEnabled: true,
                IsCoinProductRedemptionEnabled: true,
                CoinBalance: 0,
                RewardSections: [],
                History: new UserWalletBrandHistorySectionResponse("History", "No history yet.", []),
                HintText: string.Empty);

            return Task.FromResult(Result.Ok(response));
        }
    }
}
