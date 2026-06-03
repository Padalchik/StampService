using FluentResults;
using StampService.Application.Errors;
using StampService.Application.Ledger;
using StampService.Domain.Loyalty;

namespace StampService.Application.Metrics;

public class MetricLedgerService : IMetricLedgerService
{
    private readonly IMetricBalanceRepository _metricBalanceRepository;
    private readonly ILedgerOperationLock _ledgerOperationLock;
    private readonly IStampTransactionRepository _stampTransactionRepository;

    public MetricLedgerService(
        IMetricBalanceRepository metricBalanceRepository,
        IStampTransactionRepository stampTransactionRepository,
        ILedgerOperationLock? ledgerOperationLock = null)
    {
        _metricBalanceRepository = metricBalanceRepository;
        _ledgerOperationLock = ledgerOperationLock ?? NoopLedgerOperationLock.Instance;
        _stampTransactionRepository = stampTransactionRepository;
    }

    public async Task<Result<MetricLedgerOperation>> IssueAsync(
        Guid userId,
        Guid actorUserId,
        Guid brandId,
        Guid metricDefinitionId,
        int amount,
        string comment,
        CancellationToken cancellationToken)
    {
        return await _ledgerOperationLock.ExecuteWithMetricBalanceLockAsync(
            userId,
            brandId,
            metricDefinitionId,
            ct => IssueCoreAsync(userId, actorUserId, brandId, metricDefinitionId, amount, comment, ct),
            cancellationToken);
    }

    public async Task<Result<MetricLedgerOperation>> RedeemAsync(
        Guid userId,
        Guid actorUserId,
        Guid brandId,
        Guid metricDefinitionId,
        int amount,
        string comment,
        CancellationToken cancellationToken)
    {
        return await _ledgerOperationLock.ExecuteWithMetricBalanceLockAsync(
            userId,
            brandId,
            metricDefinitionId,
            ct => RedeemCoreAsync(userId, actorUserId, brandId, metricDefinitionId, amount, comment, ct),
            cancellationToken);
    }

    public async Task<Result<int>> RecalculateMetricBalanceAsync(
        Guid metricBalanceId,
        CancellationToken cancellationToken)
    {
        var balance = await _metricBalanceRepository.GetByIdAsync(metricBalanceId, cancellationToken);
        if (balance is null)
            return Result.Fail(MetricErrors.BalanceNotFound());

        return await _ledgerOperationLock.ExecuteWithMetricBalanceLockAsync(
            balance.UserId,
            balance.BrandId,
            balance.MetricDefinitionId,
            ct => RecalculateMetricBalanceCoreAsync(metricBalanceId, ct),
            cancellationToken);
    }

    private async Task<Result<MetricLedgerOperation>> IssueCoreAsync(
        Guid userId,
        Guid actorUserId,
        Guid brandId,
        Guid metricDefinitionId,
        int amount,
        string comment,
        CancellationToken cancellationToken)
    {
        var balance = await _metricBalanceRepository.GetByUserAndMetricAsync(
            userId,
            brandId,
            metricDefinitionId,
            cancellationToken);

        if (balance is null)
        {
            var balanceResult = MetricBalance.Create(userId, brandId, metricDefinitionId);
            if (balanceResult.IsFailed)
                return Result.Fail(balanceResult.Errors);

            balance = balanceResult.Value;
            _metricBalanceRepository.Add(balance);
        }
        else
        {
            var syncResult = await SynchronizeMaterializedBalanceAsync(balance, cancellationToken);
            if (syncResult.IsFailed)
                return Result.Fail(syncResult.Errors);
        }

        var transactionResult = StampTransaction.CreateIssue(balance.Id, amount, comment, actorUserId);
        if (transactionResult.IsFailed)
            return Result.Fail(transactionResult.Errors);

        var updateBalanceResult = balance.Add(amount);
        if (updateBalanceResult.IsFailed)
            return Result.Fail(updateBalanceResult.Errors);

        var transaction = transactionResult.Value;
        _stampTransactionRepository.Add(transaction);
        await _stampTransactionRepository.SaveAsync(cancellationToken);

        return Result.Ok(new MetricLedgerOperation(balance, transaction));
    }

    private async Task<Result<MetricLedgerOperation>> RedeemCoreAsync(
        Guid userId,
        Guid actorUserId,
        Guid brandId,
        Guid metricDefinitionId,
        int amount,
        string comment,
        CancellationToken cancellationToken)
    {
        var balance = await _metricBalanceRepository.GetByUserAndMetricAsync(
            userId,
            brandId,
            metricDefinitionId,
            cancellationToken);

        if (balance is null)
            return Result.Fail(MetricErrors.BalanceNotFound());

        var syncResult = await SynchronizeMaterializedBalanceAsync(balance, cancellationToken);
        if (syncResult.IsFailed)
            return Result.Fail(syncResult.Errors);

        var transactionResult = StampTransaction.CreateRedeem(balance.Id, amount, comment, actorUserId);
        if (transactionResult.IsFailed)
            return Result.Fail(transactionResult.Errors);

        var updateBalanceResult = balance.Subtract(amount);
        if (updateBalanceResult.IsFailed)
            return Result.Fail(updateBalanceResult.Errors);

        var transaction = transactionResult.Value;
        _stampTransactionRepository.Add(transaction);
        await _stampTransactionRepository.SaveAsync(cancellationToken);

        return Result.Ok(new MetricLedgerOperation(balance, transaction));
    }

    private async Task<Result<int>> RecalculateMetricBalanceCoreAsync(
        Guid metricBalanceId,
        CancellationToken cancellationToken)
    {
        var balance = await _metricBalanceRepository.GetByIdAsync(metricBalanceId, cancellationToken);
        if (balance is null)
            return Result.Fail(MetricErrors.BalanceNotFound());

        var ledgerValue = await _stampTransactionRepository.CalculateMetricBalanceValueAsync(
            metricBalanceId,
            cancellationToken);

        var setValueResult = balance.SetMaterializedValue(ledgerValue);
        if (setValueResult.IsFailed)
            return Result.Fail(setValueResult.Errors);

        await _metricBalanceRepository.SaveAsync(cancellationToken);

        return Result.Ok(balance.Value);
    }

    private async Task<Result> SynchronizeMaterializedBalanceAsync(
        MetricBalance balance,
        CancellationToken cancellationToken)
    {
        var ledgerValue = await _stampTransactionRepository.CalculateMetricBalanceValueAsync(
            balance.Id,
            cancellationToken);

        return balance.SetMaterializedValue(ledgerValue);
    }
}
