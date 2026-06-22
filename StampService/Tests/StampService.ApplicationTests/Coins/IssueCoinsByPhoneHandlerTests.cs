using StampService.Application.Access;
using StampService.Application.Brands;
using StampService.Application.Coins;
using StampService.Application.Coins.Commands.IssueCoins;
using StampService.Application.CustomerNotifications;
using StampService.Application.Users;
using StampService.ApplicationTests.Fakes;
using StampService.Contracts.DTOs.Coins;
using StampService.Domain.Access;
using StampService.Domain.Brand;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Coins;

public class IssueCoinsByPhoneHandlerTests
{
    [Fact]
    public async Task Handle_WhenPhoneUserExists_ShouldIssueCoins()
    {
        var brand = Brand.Create("Coffee").Value;
        var actorUserId = Guid.NewGuid();
        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        var userRepository = new FakeUserRepository();
        var brandCustomerRepository = new FakeBrandCustomerRepository(userRepository);
        var walletRepository = new FakeCoinWalletRepository();
        var transactionRepository = new FakeCoinTransactionRepository();
        var notificationService = new RecordingCustomerNotificationService();
        var customer = CreatePhoneUser("+79991234567");
        brandRepository.AddExisting(brand);
        userRepository.Add(customer);
        membershipRepository.SetRole(actorUserId, brand.Id, SystemRoles.Staff);

        var handler = new IssueCoinsByPhoneHandler(
            new BrandAccessService(membershipRepository),
            new BrandCustomerService(brandCustomerRepository),
            brandRepository,
            new CoinLedgerService(walletRepository, transactionRepository),
            CreatePhoneAccountService(userRepository),
            notificationService);

        var result = await handler.Handle(
            new IssueCoinsByPhoneCommand(
                brand.Id,
                actorUserId,
                new IssueCoinsByPhoneRequest("+7 999 123-45-67", 15, "Welcome coins")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(userRepository.Users);
        Assert.Equal(customer.Id, result.Value.UserId);
        Assert.Equal(customer.Name, result.Value.UserName);
        Assert.Equal(15, result.Value.Amount);
        Assert.Equal(15, result.Value.BalanceValue);
        Assert.Single(walletRepository.Wallets);
        Assert.Single(transactionRepository.Transactions);
        Assert.Single(brandCustomerRepository.Customers);
        Assert.Equal(result.Value, notificationService.CoinsIssued);
    }

    [Fact]
    public async Task Handle_WhenPhoneUserDoesNotExist_ShouldFailWithoutCreatingUser()
    {
        var brand = Brand.Create("Coffee").Value;
        var actorUserId = Guid.NewGuid();
        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        var userRepository = new FakeUserRepository();
        var brandCustomerRepository = new FakeBrandCustomerRepository(userRepository);
        var walletRepository = new FakeCoinWalletRepository();
        var transactionRepository = new FakeCoinTransactionRepository();
        brandRepository.AddExisting(brand);
        membershipRepository.SetRole(actorUserId, brand.Id, SystemRoles.Staff);

        var handler = new IssueCoinsByPhoneHandler(
            new BrandAccessService(membershipRepository),
            new BrandCustomerService(brandCustomerRepository),
            brandRepository,
            new CoinLedgerService(walletRepository, transactionRepository),
            CreatePhoneAccountService(userRepository));

        var result = await handler.Handle(
            new IssueCoinsByPhoneCommand(
                brand.Id,
                actorUserId,
                new IssueCoinsByPhoneRequest("+7 999 123-45-67", 15, "Welcome coins")),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Empty(userRepository.Users);
        Assert.Empty(walletRepository.Wallets);
        Assert.Empty(transactionRepository.Transactions);
    }

    [Fact]
    public async Task Handle_WhenActorHasNoAccess_ShouldNotCreateUser()
    {
        var brand = Brand.Create("Coffee").Value;
        var brandRepository = new FakeBrandRepository();
        var userRepository = new FakeUserRepository();
        var brandCustomerRepository = new FakeBrandCustomerRepository(userRepository);
        brandRepository.AddExisting(brand);

        var handler = new IssueCoinsByPhoneHandler(
            new BrandAccessService(new FakeBrandMembershipRepository()),
            new BrandCustomerService(brandCustomerRepository),
            brandRepository,
            new CoinLedgerService(new FakeCoinWalletRepository(), new FakeCoinTransactionRepository()),
            CreatePhoneAccountService(userRepository));

        var result = await handler.Handle(
            new IssueCoinsByPhoneCommand(
                brand.Id,
                Guid.NewGuid(),
                new IssueCoinsByPhoneRequest("+79991234567", 15, "Welcome coins")),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Empty(userRepository.Users);
    }

    [Fact]
    public async Task Handle_WhenAmountIsInvalid_ShouldNotCreateUser()
    {
        var brand = Brand.Create("Coffee").Value;
        var actorUserId = Guid.NewGuid();
        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        var userRepository = new FakeUserRepository();
        var brandCustomerRepository = new FakeBrandCustomerRepository(userRepository);
        brandRepository.AddExisting(brand);
        membershipRepository.SetRole(actorUserId, brand.Id, SystemRoles.Staff);

        var handler = new IssueCoinsByPhoneHandler(
            new BrandAccessService(membershipRepository),
            new BrandCustomerService(brandCustomerRepository),
            brandRepository,
            new CoinLedgerService(new FakeCoinWalletRepository(), new FakeCoinTransactionRepository()),
            CreatePhoneAccountService(userRepository));

        var result = await handler.Handle(
            new IssueCoinsByPhoneCommand(
                brand.Id,
                actorUserId,
                new IssueCoinsByPhoneRequest("+79991234567", 0, "Welcome coins")),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Empty(userRepository.Users);
    }

    private static PhoneAccountService CreatePhoneAccountService(FakeUserRepository repository)
    {
        return new PhoneAccountService(
            repository,
            new FixedDisplayNameGenerator());
    }

    private static User CreatePhoneUser(string phoneNumber)
    {
        var user = User.Create("Business customer").Value;
        user.AddIdentity(IdentityType.Phone, phoneNumber, "{}");
        return user;
    }

    private sealed class FixedDisplayNameGenerator : IUserDisplayNameGenerator
    {
        public string Generate() => "Business customer";
    }

    private sealed class RecordingCustomerNotificationService : ICustomerNotificationService
    {
        public CoinOperationResponse? CoinsIssued { get; private set; }

        public Task NotifyCoinsIssuedAsync(
            CoinOperationResponse operation,
            CancellationToken cancellationToken)
        {
            CoinsIssued = operation;
            return Task.CompletedTask;
        }

        public Task NotifyMetricIssuedAsync(
            StampService.Contracts.DTOs.Metrics.IssueMetricResponse operation,
            CancellationToken cancellationToken)
        {
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
            StampService.Contracts.DTOs.Metrics.RedeemMetricResponse operation,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
