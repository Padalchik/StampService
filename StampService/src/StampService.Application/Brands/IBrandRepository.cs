using FluentResults;
using StampService.Domain.Brand;

namespace StampService.Application.Brands;

public interface IBrandRepository
{
    Task<Result<Guid>> AddAsync(Brand brand, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid brandId, CancellationToken cancellationToken);
}
