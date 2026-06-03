using FluentResults;
using StampService.Application.Brands;
using StampService.Domain.Brand;

namespace StampService.ApplicationTests.Fakes;

public class FakeBrandRepository : IBrandRepository
{
    private readonly HashSet<Guid> _brandIds = [];
    private readonly Dictionary<Guid, Brand> _brands = [];

    public void AddExisting(Guid brandId)
    {
        _brandIds.Add(brandId);
    }

    public void AddExisting(Brand brand)
    {
        _brandIds.Add(brand.Id);
        _brands[brand.Id] = brand;
    }

    public Result<Guid> Add(Brand brand)
    {
        _brandIds.Add(brand.Id);
        _brands[brand.Id] = brand;
        return Result.Ok(brand.Id);
    }

    public Task<Result<Guid>> AddAsync(Brand brand, CancellationToken cancellationToken)
    {
        return Task.FromResult(Add(brand));
    }

    public Task<bool> ExistsAsync(Guid brandId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_brandIds.Contains(brandId));
    }

    public Task<Brand?> GetByIdAsync(Guid brandId, CancellationToken cancellationToken)
    {
        _brands.TryGetValue(brandId, out var brand);
        return Task.FromResult(brand);
    }

    public Task<Brand?> GetByIdForUpdateAsync(Guid brandId, CancellationToken cancellationToken)
    {
        return GetByIdAsync(brandId, cancellationToken);
    }

    public Task<IReadOnlyCollection<AdminBrandReadModel>> GetAdminBrandsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<AdminBrandReadModel> result = _brandIds
            .Select(brandId => new AdminBrandReadModel(
                brandId,
                _brands.TryGetValue(brandId, out var brand) ? brand.Name : $"Brand {brandId:N}",
                _brands.TryGetValue(brandId, out brand) ? brand.IsMetricsEnabled : true,
                _brands.TryGetValue(brandId, out brand) ? brand.IsCoinsEnabled : true,
                _brands.TryGetValue(brandId, out brand) ? brand.IsCoinProductRedemptionEnabled : true,
                _brands.TryGetValue(brandId, out brand) ? brand.IsManualCoinRedemptionEnabled : false,
                null,
                null,
                null))
            .ToArray();

        return Task.FromResult(result);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
