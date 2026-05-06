using StampService.Domain.Loyalty;

namespace StampService.Application.Metrics;

public interface IMetricBalanceRepository
{
    Task<MetricBalance?> GetByIdAsync(
        Guid metricBalanceId,
        CancellationToken cancellationToken);

    Task<MetricBalance?> GetByUserAndMetricAsync(
        Guid userId,
        Guid brandId,
        Guid metricDefinitionId,
        CancellationToken cancellationToken);

    void Add(MetricBalance balance);

    Task SaveAsync(CancellationToken cancellationToken);
}
