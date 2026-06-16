using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Brands.Commands.CreateBrandCustomerByPhone;
using StampService.Application.Brands.Queries.GetBrandCustomerCard;
using StampService.Application.Coins;
using StampService.Application.Metrics;
using StampService.Application.Users;
using StampService.ApplicationTests.Fakes;
using StampService.Contracts.DTOs.Brands;
using StampService.Contracts.DTOs.Wallet;
using StampService.Domain.Access;
using StampService.Domain.Brand;
using StampService.Domain.Loyalty;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Brands;

public class CreateBrandCustomerByPhoneHandlerTests
{
    [Fact]
    public async Task Handle_WhenWelcomeRewardsAreEnabledForNewCustomer_ShouldIssueRewardsThroughLedger()
    {
        var actor = User.Create("Staff").Value;
        var brand = Brand.Create("Coffee").Value;
        var metric = LoyaltyMetricDefinition.Create(brand.Id, "Visit", redemptionAmount: 5).Value;
        brand.UpdateWelcomeRewardSettings(
            isWelcomeRewardsEnabled: true,
            welcomeMetricRewards: [new BrandWelcomeMetricRewardSetting(metric.Id, 2)],
            welcomeCoinsAmount: 3);
        var userRepository = new FakeUserRepository();
        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        var metricRepository = new FakeLoyaltyMetricRepository();
        var metricBalanceRepository = new FakeMetricBalanceRepository();
        var stampTransactionRepository = new FakeStampTransactionRepository();
        var coinWalletRepository = new FakeCoinWalletRepository();
        var coinTransactionRepository = new FakeCoinTransactionRepository();
        userRepository.Add(actor);
        brandRepository.AddExisting(brand);
        metricRepository.AddExisting(metric);
        membershipRepository.SetRole(actor.Id, brand.Id, SystemRoles.Staff);
        var handler = CreateHandler(
            brandRepository,
            membershipRepository,
            metricRepository,
            metricBalanceRepository,
            stampTransactionRepository,
            coinWalletRepository,
            coinTransactionRepository,
            userRepository);

        var result = await handler.Handle(
            new CreateBrandCustomerByPhoneCommand(
                actor.Id,
                brand.Id,
                new CreateBrandCustomerByPhoneRequest("+79991234567")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(stampTransactionRepository.Transactions);
        Assert.Equal(2, stampTransactionRepository.Transactions.Single().Amount);
        Assert.Equal("Приветственная награда", stampTransactionRepository.Transactions.Single().Comment);
        Assert.Single(coinTransactionRepository.Transactions);
        Assert.Equal(3, coinTransactionRepository.Transactions.Single().Amount);
        Assert.Equal("Приветственная награда", coinTransactionRepository.Transactions.Single().Comment);
    }

    [Fact]
    public async Task Handle_WhenPhoneAlreadyExists_ShouldNotIssueWelcomeRewardsAgain()
    {
        var actor = User.Create("Staff").Value;
        var existingCustomer = User.Create("Customer").Value;
        existingCustomer.AddIdentity(IdentityType.Phone, "+79991234567", "{}");
        var brand = Brand.Create("Coffee").Value;
        brand.UpdateWelcomeRewardSettings(
            isWelcomeRewardsEnabled: true,
            welcomeMetricRewards: [],
            welcomeCoinsAmount: 3);
        var userRepository = new FakeUserRepository();
        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        var metricRepository = new FakeLoyaltyMetricRepository();
        var coinWalletRepository = new FakeCoinWalletRepository();
        var coinTransactionRepository = new FakeCoinTransactionRepository();
        userRepository.Add(actor);
        userRepository.Add(existingCustomer);
        brandRepository.AddExisting(brand);
        membershipRepository.SetRole(actor.Id, brand.Id, SystemRoles.Staff);
        var handler = CreateHandler(
            brandRepository,
            membershipRepository,
            metricRepository,
            new FakeMetricBalanceRepository(),
            new FakeStampTransactionRepository(),
            coinWalletRepository,
            coinTransactionRepository,
            userRepository);

        var result = await handler.Handle(
            new CreateBrandCustomerByPhoneCommand(
                actor.Id,
                brand.Id,
                new CreateBrandCustomerByPhoneRequest("+79991234567")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(coinTransactionRepository.Transactions);
    }

    private static CreateBrandCustomerByPhoneHandler CreateHandler(
        FakeBrandRepository brandRepository,
        FakeBrandMembershipRepository membershipRepository,
        FakeLoyaltyMetricRepository metricRepository,
        FakeMetricBalanceRepository metricBalanceRepository,
        FakeStampTransactionRepository stampTransactionRepository,
        FakeCoinWalletRepository coinWalletRepository,
        FakeCoinTransactionRepository coinTransactionRepository,
        FakeUserRepository userRepository)
    {
        return new CreateBrandCustomerByPhoneHandler(
            new BrandAccessService(membershipRepository),
            brandRepository,
            new MetricLedgerService(metricBalanceRepository, stampTransactionRepository),
            new CoinLedgerService(coinWalletRepository, coinTransactionRepository),
            metricRepository,
            new PhoneAccountService(userRepository, new FixedDisplayNameGenerator()),
            userRepository,
            new StubCustomerCardHandler());
    }

    private sealed class FixedDisplayNameGenerator : IUserDisplayNameGenerator
    {
        public string Generate()
        {
            return "Customer";
        }
    }

    private sealed class StubCustomerCardHandler : IQueryHandler<BrandCustomerCardResponse, GetBrandCustomerCardQuery>
    {
        public Task<Result<BrandCustomerCardResponse>> Handle(
            GetBrandCustomerCardQuery query,
            CancellationToken cancellationToken)
        {
            var response = new BrandCustomerCardResponse(
                query.BrandId,
                Guid.NewGuid(),
                "Customer",
                query.CustomerPhoneNumber,
                new UserWalletBrandDetailsResponse(
                    Guid.NewGuid(),
                    query.BrandId,
                    "Coffee",
                    IsMetricsEnabled: true,
                    IsCoinsEnabled: true,
                    IsCoinProductRedemptionEnabled: true,
                    CoinBalance: 0,
                    RewardSections: [],
                    History: new UserWalletBrandHistorySectionResponse("История", "Истории пока нет.", []),
                    HintText: string.Empty));

            return Task.FromResult(Result.Ok(response));
        }
    }
}
