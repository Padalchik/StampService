using FluentResults;
using StampService.Domain.Brand;

namespace StampService.Application.Brands;

public interface IBrandRepository
{
    Result<Guid> Add(Brand brand);

    Task<Result<Guid>> AddAsync(Brand brand, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid brandId, CancellationToken cancellationToken);

    Task<Brand?> GetByIdAsync(Guid brandId, CancellationToken cancellationToken);

    Task<Brand?> GetByIdForUpdateAsync(Guid brandId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AdminBrandReadModel>> GetAdminBrandsAsync(CancellationToken cancellationToken);

    Task SaveAsync(CancellationToken cancellationToken);
}
