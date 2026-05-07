using Microsoft.EntityFrameworkCore;
using StampService.Application.Metrics;
using StampService.Domain.Loyalty;

namespace StampService.Infrastructure.Repositories;

public class MetricBalanceRepository : IMetricBalanceRepository
{
    private readonly AppDbContext _dbContext;

    public MetricBalanceRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MetricBalance?> GetByIdAsync(
        Guid metricBalanceId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.MetricBalances
            .FirstOrDefaultAsync(balance => balance.Id == metricBalanceId, cancellationToken);
    }

    public async Task<MetricBalance?> GetByUserAndMetricAsync(
        Guid userId,
        Guid brandId,
        Guid metricDefinitionId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.MetricBalances
            .FirstOrDefaultAsync(
                balance => balance.UserId == userId
                    && balance.BrandId == brandId
                    && balance.MetricDefinitionId == metricDefinitionId,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserMetricBalanceReadModel>> GetUserBalancesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.MetricBalances
            .AsNoTracking()
            .Where(balance => balance.UserId == userId)
            .Select(balance => new UserMetricBalanceReadModel(
                balance.Id,
                balance.BrandId,
                balance.Brand.Name,
                balance.MetricDefinitionId,
                balance.MetricDefinition.Code,
                balance.MetricDefinition.Name,
                balance.Value))
            .ToArrayAsync(cancellationToken);
    }

    public void Add(MetricBalance balance)
    {
        _dbContext.MetricBalances.Add(balance);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
