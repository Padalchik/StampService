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

    public Task<IReadOnlyCollection<UserMetricBalanceReadModel>> GetUserBalancesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<UserMetricBalanceReadModel> balances = _balances
            .Where(balance => balance.UserId == userId)
            .Select(balance => new UserMetricBalanceReadModel(
                balance.Id,
                balance.BrandId,
                $"Brand {balance.BrandId:N}",
                balance.MetricDefinitionId,
                $"metric-{balance.MetricDefinitionId:N}",
                $"Metric {balance.MetricDefinitionId:N}",
                1,
                balance.Value))
            .ToArray();

        return Task.FromResult(balances);
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
