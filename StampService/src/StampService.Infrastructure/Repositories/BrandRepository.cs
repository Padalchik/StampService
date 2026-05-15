using FluentResults;
using Microsoft.EntityFrameworkCore;
using StampService.Application.Brands;
using StampService.Domain.Access;
using StampService.Domain.Brand;

namespace StampService.Infrastructure.Repositories;

public class BrandRepository : IBrandRepository
{
    private readonly AppDbContext _dbContext;

    public BrandRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<Guid>> AddAsync(Brand brand, CancellationToken cancellationToken)
    {
        _dbContext.Brands.Add(brand);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(brand.Id);
    }

    public async Task<bool> ExistsAsync(Guid brandId, CancellationToken cancellationToken)
    {
        return await _dbContext.Brands
            .AnyAsync(brand => brand.Id == brandId, cancellationToken);
    }

    public async Task<Brand?> GetByIdAsync(Guid brandId, CancellationToken cancellationToken)
    {
        return await _dbContext.Brands
            .AsNoTracking()
            .FirstOrDefaultAsync(brand => brand.Id == brandId, cancellationToken);
    }

    public async Task<Brand?> GetByIdForUpdateAsync(Guid brandId, CancellationToken cancellationToken)
    {
        return await _dbContext.Brands
            .FirstOrDefaultAsync(brand => brand.Id == brandId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AdminBrandReadModel>> GetAdminBrandsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Brands
            .AsNoTracking()
            .OrderBy(brand => brand.Name)
            .Select(brand => new AdminBrandReadModel(
                brand.Id,
                brand.Name,
                brand.IsMetricsEnabled,
                brand.IsCoinsEnabled,
                brand.IsCoinProductRedemptionEnabled,
                brand.IsManualCoinRedemptionEnabled,
                _dbContext.BrandMemberships
                    .Where(membership => membership.BrandId == brand.Id
                        && membership.Role.SystemName == SystemRoles.Owner)
                    .Select(membership => (Guid?)membership.UserId)
                    .FirstOrDefault(),
                _dbContext.BrandMemberships
                    .Where(membership => membership.BrandId == brand.Id
                        && membership.Role.SystemName == SystemRoles.Owner)
                    .Select(membership => membership.User.Name)
                    .FirstOrDefault(),
                _dbContext.BrandMemberships
                    .Where(membership => membership.BrandId == brand.Id
                        && membership.Role.SystemName == SystemRoles.Owner)
                    .Select(membership => membership.User.CustomerCode)
                    .FirstOrDefault()))
            .ToArrayAsync(cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
