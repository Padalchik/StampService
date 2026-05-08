using StampService.Application.Access;
using StampService.Application.Coins;
using StampService.Application.Coins.Commands.IssueCoins;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Access;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Coins;

public class IssueCoinsHandlerTests
{
    [Fact]
    public async Task Handle_WhenActorCanIssue_ShouldCreateCoinWalletAndTransaction()
    {
        var brandId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var customer = User.Create("Customer", "1234").Value;
        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        var userRepository = new FakeUserRepository();
        var walletRepository = new FakeCoinWalletRepository();
        var transactionRepository = new FakeCoinTransactionRepository();
        brandRepository.AddExisting(brandId);
        membershipRepository.SetRole(actorUserId, brandId, SystemRoles.Staff);
        userRepository.Add(customer);

        var handler = new IssueCoinsHandler(
            new BrandAccessService(membershipRepository),
            brandRepository,
            new CoinLedgerService(walletRepository, transactionRepository),
            userRepository);

        var result = await handler.Handle(
            new IssueCoinsCommand(brandId, actorUserId, "1234", 15, "Welcome coins"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(customer.Id, result.Value.UserId);
        Assert.Equal(15, result.Value.Amount);
        Assert.Equal(15, result.Value.BalanceValue);
        Assert.Single(walletRepository.Wallets);
        Assert.Single(transactionRepository.Transactions);
    }

    [Fact]
    public async Task Handle_WhenActorHasNoAccess_ShouldFail()
    {
        var brandId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var customer = User.Create("Customer", "1234").Value;
        var brandRepository = new FakeBrandRepository();
        var userRepository = new FakeUserRepository();
        brandRepository.AddExisting(brandId);
        userRepository.Add(customer);

        var handler = new IssueCoinsHandler(
            new BrandAccessService(new FakeBrandMembershipRepository()),
            brandRepository,
            new CoinLedgerService(new FakeCoinWalletRepository(), new FakeCoinTransactionRepository()),
            userRepository);

        var result = await handler.Handle(
            new IssueCoinsCommand(brandId, actorUserId, "1234", 15, "Welcome coins"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }
}
