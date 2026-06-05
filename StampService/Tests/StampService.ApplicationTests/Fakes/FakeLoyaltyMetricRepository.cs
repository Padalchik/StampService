using StampService.Application.Metrics;
using StampService.Domain.Loyalty;

namespace StampService.ApplicationTests.Fakes;

public class FakeLoyaltyMetricRepository : ILoyaltyMetricRepository
{
    private readonly List<LoyaltyMetricDefinition> _metrics = [];

    public int SaveCount { get; private set; }

    public void AddExisting(LoyaltyMetricDefinition metric)
    {
        _metrics.Add(metric);
    }

    public Task<LoyaltyMetricDefinition?> GetByIdAsync(
        Guid metricDefinitionId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_metrics.FirstOrDefault(metric => metric.Id == metricDefinitionId));
    }

    public Task<LoyaltyMetricDefinition?> GetByIdForUpdateAsync(
        Guid metricDefinitionId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_metrics.FirstOrDefault(metric => metric.Id == metricDefinitionId));
    }

    public Task<IReadOnlyCollection<LoyaltyMetricDefinition>> GetByBrandAsync(
        Guid brandId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<LoyaltyMetricDefinition> result = _metrics
            .Where(metric => metric.BrandId == brandId)
            .OrderBy(metric => metric.Name)
            .ToArray();

        return Task.FromResult(result);
    }

    public void Add(LoyaltyMetricDefinition metric)
    {
        _metrics.Add(metric);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
