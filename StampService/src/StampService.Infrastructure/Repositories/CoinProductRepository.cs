using Microsoft.EntityFrameworkCore;
using StampService.Application.CoinProducts;
using StampService.Domain.Coins;

namespace StampService.Infrastructure.Repositories;

public class CoinProductRepository : ICoinProductRepository
{
    private readonly AppDbContext _dbContext;

    public CoinProductRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CoinProduct?> GetByIdAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CoinProducts
            .AsNoTracking()
            .FirstOrDefaultAsync(product => product.Id == productId, cancellationToken);
    }

    public async Task<CoinProduct?> GetByIdForUpdateAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CoinProducts
            .FirstOrDefaultAsync(product => product.Id == productId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CoinProduct>> GetByBrandAsync(
        Guid brandId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CoinProducts
            .AsNoTracking()
            .Where(product => product.BrandId == brandId)
            .OrderBy(product => product.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CoinProduct>> GetActiveByBrandAsync(
        Guid brandId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CoinProducts
            .AsNoTracking()
            .Where(product => product.BrandId == brandId && product.IsActive)
            .OrderBy(product => product.Name)
            .ToArrayAsync(cancellationToken);
    }

    public void Add(CoinProduct product)
    {
        _dbContext.CoinProducts.Add(product);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
