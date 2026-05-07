using StampService.Application.Metrics;
using StampService.Domain.Loyalty;

namespace StampService.ApplicationTests.Fakes;

public class FakeLoyaltyMetricRepository : ILoyaltyMetricRepository
{
    private readonly List<LoyaltyMetricDefinition> _metrics = [];

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

    public Task<bool> CodeExistsAsync(
        Guid brandId,
        string code,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_metrics.Any(metric =>
            metric.BrandId == brandId && metric.Code == code));
    }

    public void Add(LoyaltyMetricDefinition metric)
    {
        _metrics.Add(metric);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
