using FluentResults;
using StampService.Application.Brands;
using StampService.Domain.Brand;

namespace StampService.ApplicationTests.Fakes;

public class FakeBrandRepository : IBrandRepository
{
    private readonly HashSet<Guid> _brandIds = [];

    public void AddExisting(Guid brandId)
    {
        _brandIds.Add(brandId);
    }

    public Task<Result<Guid>> AddAsync(Brand brand, CancellationToken cancellationToken)
    {
        _brandIds.Add(brand.Id);
        return Task.FromResult(Result.Ok(brand.Id));
    }

    public Task<bool> ExistsAsync(Guid brandId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_brandIds.Contains(brandId));
    }
}
