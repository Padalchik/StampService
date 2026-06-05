using FluentResults;
using Microsoft.EntityFrameworkCore;
using StampService.Application.Ledger;

namespace StampService.Infrastructure.Services;

public sealed class PostgresLedgerOperationLock : ILedgerOperationLock
{
    private readonly AppDbContext _dbContext;

    public PostgresLedgerOperationLock(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Result<T>> ExecuteWithMetricBalanceLockAsync<T>(
        Guid userId,
        Guid brandId,
        Guid metricDefinitionId,
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken)
    {
        var lockKey = $"metric-balance:{userId:N}:{brandId:N}:{metricDefinitionId:N}";
        return ExecuteWithLockAsync(lockKey, operation, cancellationToken);
    }

    public Task<Result<T>> ExecuteWithCoinWalletLockAsync<T>(
        Guid userId,
        Guid brandId,
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken)
    {
        var lockKey = $"coin-wallet:{userId:N}:{brandId:N}";
        return ExecuteWithLockAsync(lockKey, operation, cancellationToken);
    }

    private async Task<Result<T>> ExecuteWithLockAsync<T>(
        string lockKey,
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken)
    {
        if (_dbContext.Database.CurrentTransaction is not null)
        {
            await AcquireLockAsync(lockKey, cancellationToken);
            return await operation(cancellationToken);
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await AcquireLockAsync(lockKey, cancellationToken);

        var result = await operation(cancellationToken);
        if (result.IsFailed)
        {
            await transaction.RollbackAsync(cancellationToken);
            return result;
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task AcquireLockAsync(string lockKey, CancellationToken cancellationToken)
    {
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))",
            cancellationToken);
    }
}
