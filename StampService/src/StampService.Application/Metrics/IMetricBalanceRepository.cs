using StampService.Domain.Loyalty;

namespace StampService.Application.Metrics;

public interface IMetricBalanceRepository
{
    Task<MetricBalance?> GetByUserAndMetricAsync(
        Guid userId,
        Guid brandId,
        Guid metricDefinitionId,
        CancellationToken cancellationToken);

    void Add(MetricBalance balance);
}
