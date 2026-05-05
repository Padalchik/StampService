using Microsoft.EntityFrameworkCore;
using StampService.Application.Metrics;
using StampService.Domain.Loyalty;

namespace StampService.Infrastructure.Repositories;

public class LoyaltyMetricRepository : ILoyaltyMetricRepository
{
    private readonly AppDbContext _dbContext;

    public LoyaltyMetricRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LoyaltyMetricDefinition?> GetByIdAsync(
        Guid metricDefinitionId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.LoyaltyMetricDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(metric => metric.Id == metricDefinitionId, cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(
        Guid brandId,
        string code,
        CancellationToken cancellationToken)
    {
        return await _dbContext.LoyaltyMetricDefinitions
            .AsNoTracking()
            .AnyAsync(
                metric => metric.BrandId == brandId && metric.Code == code,
                cancellationToken);
    }

    public void Add(LoyaltyMetricDefinition metric)
    {
        _dbContext.LoyaltyMetricDefinitions.Add(metric);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
