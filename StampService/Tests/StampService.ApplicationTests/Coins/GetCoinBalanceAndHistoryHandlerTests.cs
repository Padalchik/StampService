using StampService.Application.Access;
using StampService.Application.Coins.Queries.GetCoinBalance;
using StampService.Application.Coins.Queries.GetCoinHistory;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Access;
using StampService.Domain.Coins;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Coins;

public class GetCoinBalanceAndHistoryHandlerTests
{
    [Fact]
    public async Task GetCoinBalance_WhenWalletDoesNotExist_ShouldReturnZeroBalance()
    {
        var brandId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var customer = User.Create("Customer", "1234").Value;
        var membershipRepository = new FakeBrandMembershipRepository();
        var userRepository = new FakeUserRepository();
        membershipRepository.SetRole(actorUserId, brandId, SystemRoles.Staff);
        userRepository.Add(customer);

        var handler = new GetCoinBalanceHandler(
            new BrandAccessService(membershipRepository),
            new FakeCoinWalletRepository(),
            userRepository);

        var result = await handler.Handle(
            new GetCoinBalanceQuery(brandId, actorUserId, customer.CustomerCode),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.WalletId);
        Assert.Equal(0, result.Value.Value);
    }

    [Fact]
    public async Task GetCoinHistory_WhenWalletExists_ShouldReturnTransactionsNewestFirst()
    {
        var brandId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var customer = User.Create("Customer", "1234").Value;
        var membershipRepository = new FakeBrandMembershipRepository();
        var userRepository = new FakeUserRepository();
        var walletRepository = new FakeCoinWalletRepository();
        var transactionRepository = new FakeCoinTransactionRepository();
        membershipRepository.SetRole(actorUserId, brandId, SystemRoles.Staff);
        userRepository.Add(customer);
        var wallet = CoinWallet.Create(customer.Id, brandId).Value;
        walletRepository.Add(wallet);
        transactionRepository.Add(CoinTransaction.CreateIssue(wallet.Id, 10, "Issue").Value);
        transactionRepository.Add(CoinTransaction.CreateRedeem(wallet.Id, 4, "Redeem").Value);

        var handler = new GetCoinHistoryHandler(
            new BrandAccessService(membershipRepository),
            walletRepository,
            transactionRepository,
            userRepository);

        var result = await handler.Handle(
            new GetCoinHistoryQuery(brandId, actorUserId, customer.CustomerCode, Skip: 0, Take: 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Contains(result.Value.Items, item => item.TransactionType == CoinTransactionType.Issue.ToString());
        Assert.Contains(result.Value.Items, item => item.TransactionType == CoinTransactionType.Redeem.ToString());
    }
}
