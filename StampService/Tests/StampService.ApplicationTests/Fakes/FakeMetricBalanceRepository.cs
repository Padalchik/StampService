using StampService.Application.Metrics;
using StampService.Domain.Loyalty;

namespace StampService.ApplicationTests.Fakes;

public class FakeMetricBalanceRepository : IMetricBalanceRepository
{
    private readonly List<MetricBalance> _balances = [];

    public IReadOnlyCollection<MetricBalance> Balances => _balances;

    public Task<MetricBalance?> GetByIdAsync(
        Guid metricBalanceId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_balances.FirstOrDefault(balance => balance.Id == metricBalanceId));
    }

    public Task<MetricBalance?> GetByUserAndMetricAsync(
        Guid userId,
        Guid brandId,
        Guid metricDefinitionId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_balances.FirstOrDefault(balance =>
            balance.UserId == userId
            && balance.BrandId == brandId
            && balance.MetricDefinitionId == metricDefinitionId));
    }

    public void Add(MetricBalance balance)
    {
        _balances.Add(balance);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
