using FluentResults;
using StampService.Domain.Brand;

namespace StampService.Application.Brands;

public interface IBrandService
{
    Task<Result<Guid>> AddAsync(Brand brand, CancellationToken cancellationToken);
}
