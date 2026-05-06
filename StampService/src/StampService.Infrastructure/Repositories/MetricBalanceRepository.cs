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

    public void Add(MetricBalance balance)
    {
        _dbContext.MetricBalances.Add(balance);
    }
}
