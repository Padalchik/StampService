using FluentResults;
using StampService.Contracts.DTOs.Brands;

namespace StampService.Application.Brand;

public interface IBrandService
{
    Task<Result<CreateBrandResponse>> CreateAsync(CreateBrandRequest request, Guid userId);
}
