using FluentResults;
using StampService.Application.Brands;
using StampService.Domain.Brand;

namespace StampService.Infrastructure.Repositories;

public class BrandService : IBrandService
{
    private readonly AppDbContext _dbContext;

    public BrandService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<Guid>> AddAsync(Brand brand, CancellationToken cancellationToken)
    {
        _dbContext.Brands.Add(brand);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(brand.Id);
    }
}
