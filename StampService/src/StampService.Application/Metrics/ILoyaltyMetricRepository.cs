using StampService.Domain.Loyalty;

namespace StampService.Application.Metrics;

public interface ILoyaltyMetricRepository
{
    Task<LoyaltyMetricDefinition?> GetByIdAsync(
        Guid metricDefinitionId,
        CancellationToken cancellationToken);

    Task<LoyaltyMetricDefinition?> GetByIdForUpdateAsync(
        Guid metricDefinitionId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LoyaltyMetricDefinition>> GetByBrandAsync(
        Guid brandId,
        CancellationToken cancellationToken);

    void Add(LoyaltyMetricDefinition metric);

    Task SaveAsync(CancellationToken cancellationToken);
}
