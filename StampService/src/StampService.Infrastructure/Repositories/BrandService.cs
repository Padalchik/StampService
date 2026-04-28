using FluentResults;
using StampService.Application.Brand;
using StampService.Contracts.DTOs.Brands;
using StampService.Domain.Brand;

namespace StampService.Infrastructure.Repositories;

public class BrandService : IBrandService
{
    private readonly AppDbContext _dbContext;

    public BrandService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CreateBrandResponse>> CreateAsync(CreateBrandRequest request, Guid userId)
    {
        var brandResult = Brand.Create(request.Name);
        if (brandResult.IsFailed)
            return Result.Fail(brandResult.Errors);

        var brand = brandResult.Value;

        _dbContext.Brands.Add(brand);
        await _dbContext.SaveChangesAsync();

        var response = new CreateBrandResponse(brand.Id, brand.Name, brand.CreatedAt);

        return Result.Ok(response);
    }
}
