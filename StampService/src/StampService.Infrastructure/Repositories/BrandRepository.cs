using FluentResults;
using Microsoft.EntityFrameworkCore;
using StampService.Application.Brands;
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
}
