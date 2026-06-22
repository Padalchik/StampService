using FluentResults;
using Microsoft.EntityFrameworkCore;
using StampService.Application.Brands;
using StampService.Domain.Access;
using StampService.Domain.Brand;
using StampService.Domain.User;

namespace StampService.Infrastructure.Repositories;

public class BrandRepository : IBrandRepository
{
    private readonly AppDbContext _dbContext;

    public BrandRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Result<Guid> Add(Brand brand)
    {
        _dbContext.Brands.Add(brand);
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
            .Include(brand => brand.WelcomeMetricRewards)
            .FirstOrDefaultAsync(brand => brand.Id == brandId, cancellationToken);
    }

    public async Task<Brand?> GetByIdForUpdateAsync(Guid brandId, CancellationToken cancellationToken)
    {
        return await _dbContext.Brands
            .Include(brand => brand.WelcomeMetricRewards)
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
                    .Select(membership => membership.User.Identities
                        .Where(identity => identity.Type == IdentityType.Phone)
                        .Select(identity => identity.Key)
                        .FirstOrDefault())
                    .FirstOrDefault()))
            .ToArrayAsync(cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
