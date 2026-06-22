using Microsoft.Extensions.Options;
using StampService.Application.Administration;
using StampService.Application.Brands;
using StampService.Application.Coins;
using StampService.Application.CustomerNotifications;
using StampService.Application.Demo.Commands.CreateUserDemoData;
using StampService.Application.Metrics;
using StampService.ApplicationTests.Fakes;
using StampService.Contracts.DTOs.Coins;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Access;
using StampService.Domain.Brand;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Demo;

public class CreateUserDemoDataHandlerTests
{
    [Fact]
    public async Task Handle_WhenDemoLedgerIsCreated_ShouldNotifyCustomerAboutIssuedRewards()
    {
        var adminPhoneNumber = "+79214408362";
        var customerPhoneNumber = "+79214408363";
        var admin = User.Create("Admin").Value;
        admin.AddIdentity(IdentityType.Phone, adminPhoneNumber, "{}");
        var customer = User.Create("Customer").Value;
        customer.AddIdentity(IdentityType.Phone, customerPhoneNumber, "{}");

        var userRepository = new FakeUserRepository();
        userRepository.Add(admin);
        userRepository.Add(customer);

        var brand = Brand.Create("Coffee").Value;
        var brandRepository = new FakeBrandRepository();
        brandRepository.AddExisting(brand);

        var membershipRepository = new FakeBrandMembershipRepository();
        membershipRepository.SetRole(admin.Id, brand.Id, SystemRoles.Owner, brand.Name);

        var metricBalanceRepository = new FakeMetricBalanceRepository();
        var stampTransactionRepository = new FakeStampTransactionRepository();
        var coinWalletRepository = new FakeCoinWalletRepository();
        var coinTransactionRepository = new FakeCoinTransactionRepository();
        var notificationService = new RecordingCustomerNotificationService();

        var handler = new CreateUserDemoDataHandler(
            new AdminAccessService(Options.Create(new AdminOptions
            {
                PhoneNumbers = [adminPhoneNumber]
            }), userRepository),
            new BrandCustomerService(new FakeBrandCustomerRepository(userRepository)),
            membershipRepository,
            brandRepository,
            new CoinLedgerService(coinWalletRepository, coinTransactionRepository),
            new FakeCoinProductRepository(),
            new FakeLoyaltyMetricRepository(),
            new MetricLedgerService(metricBalanceRepository, stampTransactionRepository),
            userRepository,
            notificationService);

        var result = await handler.Handle(
            new CreateUserDemoDataCommand(
                AdminActor.FromUser(admin.Id),
                customerPhoneNumber,
                brand.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, notificationService.IssuedCoins.Count);
        Assert.Equal(3, notificationService.IssuedMetrics.Count);
        Assert.All(notificationService.IssuedCoins, operation =>
        {
            Assert.Equal(customer.Id, operation.UserId);
            Assert.Equal(brand.Id, operation.BrandId);
        });
        Assert.All(notificationService.IssuedMetrics, operation =>
        {
            Assert.Equal(customer.Id, operation.UserId);
            Assert.Equal(brand.Id, operation.BrandId);
        });
    }

    private sealed class RecordingCustomerNotificationService : ICustomerNotificationService
    {
        public List<CoinOperationResponse> IssuedCoins { get; } = [];
        public List<IssueMetricResponse> IssuedMetrics { get; } = [];

        public Task NotifyCoinsIssuedAsync(
            CoinOperationResponse operation,
            CancellationToken cancellationToken)
        {
            IssuedCoins.Add(operation);
            return Task.CompletedTask;
        }

        public Task NotifyMetricIssuedAsync(
            IssueMetricResponse operation,
            CancellationToken cancellationToken)
        {
            IssuedMetrics.Add(operation);
            return Task.CompletedTask;
        }

        public Task NotifyCoinsRedeemedAsync(
            CoinOperationResponse operation,
            string comment,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task NotifyCoinProductPurchasedAsync(
            CoinOperationResponse operation,
            string productName,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task NotifyMetricRedeemedAsync(
            RedeemMetricResponse operation,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
