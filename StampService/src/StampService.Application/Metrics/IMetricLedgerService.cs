using FluentResults;

namespace StampService.Application.Metrics;

public interface IMetricLedgerService
{
    Task<Result<MetricLedgerOperation>> IssueAsync(
        Guid userId,
        Guid actorUserId,
        Guid brandId,
        Guid metricDefinitionId,
        int amount,
        string comment,
        CancellationToken cancellationToken);

    Task<Result<MetricLedgerOperation>> RedeemAsync(
        Guid userId,
        Guid actorUserId,
        Guid brandId,
        Guid metricDefinitionId,
        int amount,
        string comment,
        CancellationToken cancellationToken);

    Task<Result<int>> RecalculateMetricBalanceAsync(
        Guid metricBalanceId,
        CancellationToken cancellationToken);
}
