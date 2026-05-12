using StampService.Application.Access;
using StampService.Application.Metrics.Queries.GetBrandCustomerMetricBalances;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Access;
using StampService.Domain.Coins;
using StampService.Domain.Loyalty;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Metrics;

public class GetBrandCustomerMetricBalancesHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserCanViewBalances_ShouldReturnBrandMetricsWithCustomerBalances()
    {
        var brandId = Guid.NewGuid();
        var otherBrandId = Guid.NewGuid();
        var staff = User.Create("Staff", "1001").Value;
        var customer = User.Create("Customer", "2002").Value;
        var activeMetric = LoyaltyMetricDefinition.Create(brandId, "coffee", "Coffee", 1).Value;
        var inactiveMetric = LoyaltyMetricDefinition.Create(brandId, "cake", "Cake", 1).Value;
        var otherBrandMetric = LoyaltyMetricDefinition.Create(otherBrandId, "tea", "Tea", 1).Value;
        inactiveMetric.Deactivate();

        var userRepository = new FakeUserRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        var metricRepository = new FakeLoyaltyMetricRepository();
        var balanceRepository = new FakeMetricBalanceRepository();
        var coinWalletRepository = new FakeCoinWalletRepository();

        userRepository.Add(staff);
        userRepository.Add(customer);
        membershipRepository.SetRole(staff.Id, brandId, SystemRoles.Staff);
        metricRepository.AddExisting(activeMetric);
        metricRepository.AddExisting(inactiveMetric);
        metricRepository.AddExisting(otherBrandMetric);

        var balance = MetricBalance.Create(customer.Id, brandId, activeMetric.Id).Value;
        balance.SetMaterializedValue(7);
        balanceRepository.Add(balance);
        var coinWallet = CoinWallet.Create(customer.Id, brandId).Value;
        coinWallet.Add(11);
        coinWalletRepository.Add(coinWallet);

        var handler = new GetBrandCustomerMetricBalancesHandler(
            new BrandAccessService(membershipRepository),
            metricRepository,
            balanceRepository,
            coinWalletRepository,
            userRepository);

        var result = await handler.Handle(
            new GetBrandCustomerMetricBalancesQuery(staff.Id, brandId, customer.CustomerCode),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(customer.Id, result.Value.CustomerUserId);
        Assert.Equal(customer.CustomerCode, result.Value.CustomerCode);
        Assert.Equal(11, result.Value.CoinBalanceValue);
        Assert.Equal(2, result.Value.Balances.Count);
        Assert.DoesNotContain(result.Value.Balances, item => item.MetricDefinitionId == otherBrandMetric.Id);

        var active = result.Value.Balances.Single(item => item.MetricDefinitionId == activeMetric.Id);
        Assert.True(active.IsActive);
        Assert.Equal(7, active.Value);

        var inactive = result.Value.Balances.Single(item => item.MetricDefinitionId == inactiveMetric.Id);
        Assert.False(inactive.IsActive);
        Assert.Equal(0, inactive.Value);
    }

    [Fact]
    public async Task Handle_WhenUserCannotViewBalances_ShouldFail()
    {
        var handler = new GetBrandCustomerMetricBalancesHandler(
            new BrandAccessService(new FakeBrandMembershipRepository()),
            new FakeLoyaltyMetricRepository(),
            new FakeMetricBalanceRepository(),
            new FakeCoinWalletRepository(),
            new FakeUserRepository());

        var result = await handler.Handle(
            new GetBrandCustomerMetricBalancesQuery(Guid.NewGuid(), Guid.NewGuid(), "1234"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task Handle_WhenCustomerHasNoCoinWallet_ShouldReturnZeroCoinBalance()
    {
        var brandId = Guid.NewGuid();
        var staff = User.Create("Staff", "1001").Value;
        var customer = User.Create("Customer", "2002").Value;
        var userRepository = new FakeUserRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        userRepository.Add(staff);
        userRepository.Add(customer);
        membershipRepository.SetRole(staff.Id, brandId, SystemRoles.Staff);

        var handler = new GetBrandCustomerMetricBalancesHandler(
            new BrandAccessService(membershipRepository),
            new FakeLoyaltyMetricRepository(),
            new FakeMetricBalanceRepository(),
            new FakeCoinWalletRepository(),
            userRepository);

        var result = await handler.Handle(
            new GetBrandCustomerMetricBalancesQuery(staff.Id, brandId, customer.CustomerCode),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.CoinBalanceValue);
    }
}
