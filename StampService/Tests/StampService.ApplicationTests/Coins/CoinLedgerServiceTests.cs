using StampService.Application.Coins;
using StampService.Application.Errors;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Coins;

namespace StampService.ApplicationTests.Coins;

public class CoinLedgerServiceTests
{
    [Fact]
    public async Task IssueAsync_WhenWalletDoesNotExist_ShouldCreateWalletAndIssueTransaction()
    {
        var walletRepository = new FakeCoinWalletRepository();
        var transactionRepository = new FakeCoinTransactionRepository();
        var service = new CoinLedgerService(walletRepository, transactionRepository);

        var result = await service.IssueAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            10,
            "Welcome coins",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(walletRepository.Wallets);
        Assert.Single(transactionRepository.Transactions);
        Assert.Equal(10, result.Value.Wallet.Value);
        Assert.Equal(CoinTransactionType.Issue, result.Value.Transaction.Type);
        Assert.Equal(10, result.Value.Transaction.Amount);
    }

    [Fact]
    public async Task IssueAsync_WhenWalletExists_ShouldIncreaseBalance()
    {
        var userId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var walletRepository = new FakeCoinWalletRepository();
        var transactionRepository = new FakeCoinTransactionRepository();
        var wallet = CoinWallet.Create(userId, brandId).Value;
        wallet.Add(3);
        walletRepository.Add(wallet);
        transactionRepository.Add(CoinTransaction.CreateIssue(wallet.Id, 3, "Existing issue").Value);
        var service = new CoinLedgerService(walletRepository, transactionRepository);

        var result = await service.IssueAsync(
            userId,
            brandId,
            7,
            "Purchase reward",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(walletRepository.Wallets);
        Assert.Equal(10, result.Value.Wallet.Value);
        Assert.Equal(2, transactionRepository.Transactions.Count);
    }

    [Fact]
    public async Task RedeemAsync_WhenWalletHasEnoughCoins_ShouldDecreaseBalanceAndCreateTransaction()
    {
        var userId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var walletRepository = new FakeCoinWalletRepository();
        var transactionRepository = new FakeCoinTransactionRepository();
        var wallet = CoinWallet.Create(userId, brandId).Value;
        wallet.Add(10);
        walletRepository.Add(wallet);
        transactionRepository.Add(CoinTransaction.CreateIssue(wallet.Id, 10, "Existing issue").Value);
        var service = new CoinLedgerService(walletRepository, transactionRepository);

        var result = await service.RedeemAsync(
            userId,
            brandId,
            4,
            "Free product",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(6, result.Value.Wallet.Value);
        Assert.Equal(2, transactionRepository.Transactions.Count);
        Assert.Equal(CoinTransactionType.Redeem, result.Value.Transaction.Type);
    }

    [Fact]
    public async Task RedeemAsync_WhenWalletDoesNotExist_ShouldFail()
    {
        var service = new CoinLedgerService(
            new FakeCoinWalletRepository(),
            new FakeCoinTransactionRepository());

        var result = await service.RedeemAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "Free product",
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, error => error.Message == "Coin wallet not found");
    }

    [Fact]
    public async Task RedeemAsync_WhenBalanceIsInsufficient_ShouldFailAndNotCreateTransaction()
    {
        var userId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var walletRepository = new FakeCoinWalletRepository();
        var transactionRepository = new FakeCoinTransactionRepository();
        var wallet = CoinWallet.Create(userId, brandId).Value;
        wallet.Add(2);
        walletRepository.Add(wallet);
        transactionRepository.Add(CoinTransaction.CreateIssue(wallet.Id, 2, "Existing issue").Value);
        var service = new CoinLedgerService(walletRepository, transactionRepository);

        var result = await service.RedeemAsync(
            userId,
            brandId,
            3,
            "Free product",
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(AppErrorCodes.Coin.InsufficientFunds, ((AppError)result.Errors[0]).Code);
        Assert.Single(transactionRepository.Transactions);
        Assert.Equal(2, wallet.Value);
    }

    [Fact]
    public async Task IssueAsync_WhenTransactionIsInvalid_ShouldNotChangeExistingWallet()
    {
        var userId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var walletRepository = new FakeCoinWalletRepository();
        var transactionRepository = new FakeCoinTransactionRepository();
        var wallet = CoinWallet.Create(userId, brandId).Value;
        wallet.Add(2);
        walletRepository.Add(wallet);
        transactionRepository.Add(CoinTransaction.CreateIssue(wallet.Id, 2, "Existing issue").Value);
        var service = new CoinLedgerService(walletRepository, transactionRepository);

        var result = await service.IssueAsync(
            userId,
            brandId,
            1,
            string.Empty,
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(transactionRepository.Transactions);
        Assert.Equal(2, wallet.Value);
    }

    [Fact]
    public async Task IssueAsync_WhenTransactionIsInvalidForNewWallet_ShouldNotAddWallet()
    {
        var walletRepository = new FakeCoinWalletRepository();
        var transactionRepository = new FakeCoinTransactionRepository();
        var service = new CoinLedgerService(walletRepository, transactionRepository);

        var result = await service.IssueAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            string.Empty,
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Empty(walletRepository.Wallets);
        Assert.Empty(transactionRepository.Transactions);
    }

    [Fact]
    public async Task RedeemAsync_WhenTransactionIsInvalid_ShouldNotChangeWallet()
    {
        var userId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var walletRepository = new FakeCoinWalletRepository();
        var transactionRepository = new FakeCoinTransactionRepository();
        var wallet = CoinWallet.Create(userId, brandId).Value;
        wallet.Add(5);
        walletRepository.Add(wallet);
        transactionRepository.Add(CoinTransaction.CreateIssue(wallet.Id, 5, "Existing issue").Value);
        var service = new CoinLedgerService(walletRepository, transactionRepository);

        var result = await service.RedeemAsync(
            userId,
            brandId,
            1,
            string.Empty,
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(transactionRepository.Transactions);
        Assert.Equal(5, wallet.Value);
    }
}
