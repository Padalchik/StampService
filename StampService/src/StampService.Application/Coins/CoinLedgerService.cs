using FluentResults;
using StampService.Application.Errors;
using StampService.Domain.Coins;

namespace StampService.Application.Coins;

public class CoinLedgerService : ICoinLedgerService
{
    private readonly ICoinWalletRepository _coinWalletRepository;
    private readonly ICoinTransactionRepository _coinTransactionRepository;

    public CoinLedgerService(
        ICoinWalletRepository coinWalletRepository,
        ICoinTransactionRepository coinTransactionRepository)
    {
        _coinWalletRepository = coinWalletRepository;
        _coinTransactionRepository = coinTransactionRepository;
    }

    public async Task<Result<CoinLedgerOperation>> IssueAsync(
        Guid userId,
        Guid brandId,
        int amount,
        string comment,
        CancellationToken cancellationToken)
    {
        var wallet = await _coinWalletRepository.GetByUserAndBrandAsync(
            userId,
            brandId,
            cancellationToken);

        var shouldAddWallet = false;
        if (wallet is null)
        {
            var walletResult = CoinWallet.Create(userId, brandId);
            if (walletResult.IsFailed)
                return Result.Fail(walletResult.Errors);

            wallet = walletResult.Value;
            shouldAddWallet = true;
        }
        else
        {
            var syncResult = await SynchronizeMaterializedWalletAsync(wallet, cancellationToken);
            if (syncResult.IsFailed)
                return Result.Fail(syncResult.Errors);
        }

        var transactionResult = CoinTransaction.CreateIssue(wallet.Id, amount, comment);
        if (transactionResult.IsFailed)
            return Result.Fail(transactionResult.Errors);

        var addResult = wallet.Add(amount);
        if (addResult.IsFailed)
            return Result.Fail(addResult.Errors);

        var transaction = transactionResult.Value;
        if (shouldAddWallet)
            _coinWalletRepository.Add(wallet);

        _coinTransactionRepository.Add(transaction);
        await _coinTransactionRepository.SaveAsync(cancellationToken);

        return Result.Ok(new CoinLedgerOperation(wallet, transaction));
    }

    public async Task<Result<CoinLedgerOperation>> RedeemAsync(
        Guid userId,
        Guid brandId,
        int amount,
        string comment,
        CancellationToken cancellationToken)
    {
        var wallet = await _coinWalletRepository.GetByUserAndBrandAsync(
            userId,
            brandId,
            cancellationToken);

        if (wallet is null)
            return Result.Fail(CoinErrors.WalletNotFound());

        var syncResult = await SynchronizeMaterializedWalletAsync(wallet, cancellationToken);
        if (syncResult.IsFailed)
            return Result.Fail(syncResult.Errors);

        if (wallet.Value < amount)
            return Result.Fail(CoinErrors.InsufficientFunds(wallet.Value, amount));

        var transactionResult = CoinTransaction.CreateRedeem(wallet.Id, amount, comment);
        if (transactionResult.IsFailed)
            return Result.Fail(transactionResult.Errors);

        var subtractResult = wallet.Subtract(amount);
        if (subtractResult.IsFailed)
            return Result.Fail(subtractResult.Errors);

        var transaction = transactionResult.Value;
        _coinTransactionRepository.Add(transaction);
        await _coinTransactionRepository.SaveAsync(cancellationToken);

        return Result.Ok(new CoinLedgerOperation(wallet, transaction));
    }

    private async Task<Result> SynchronizeMaterializedWalletAsync(
        CoinWallet wallet,
        CancellationToken cancellationToken)
    {
        var ledgerValue = await _coinTransactionRepository.CalculateWalletValueAsync(
            wallet.Id,
            cancellationToken);

        return wallet.SetMaterializedValue(ledgerValue);
    }
}
