using StampService.Application.Metrics;
using StampService.Domain.Loyalty;

namespace StampService.ApplicationTests.Fakes;

public class FakeMetricBalanceRepository : IMetricBalanceRepository
{
    private readonly List<MetricBalance> _balances = [];
    private readonly Dictionary<Guid, MetricReadModel> _metricReadModels = [];

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
                GetMetricName(balance.MetricDefinitionId),
                GetRedemptionAmount(balance.MetricDefinitionId),
                balance.Value))
            .ToArray();

        return Task.FromResult(balances);
    }

    public void Add(MetricBalance balance)
    {
        _balances.Add(balance);
    }

    public void SetMetricReadModel(Guid metricDefinitionId, string metricName, int redemptionAmount)
    {
        _metricReadModels[metricDefinitionId] = new MetricReadModel(metricName, redemptionAmount);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private string GetMetricName(Guid metricDefinitionId)
    {
        return _metricReadModels.TryGetValue(metricDefinitionId, out var readModel)
            ? readModel.Name
            : $"Metric {metricDefinitionId:N}";
    }

    private int GetRedemptionAmount(Guid metricDefinitionId)
    {
        return _metricReadModels.TryGetValue(metricDefinitionId, out var readModel)
            ? readModel.RedemptionAmount
            : 1;
    }

    private sealed record MetricReadModel(string Name, int RedemptionAmount);
}
