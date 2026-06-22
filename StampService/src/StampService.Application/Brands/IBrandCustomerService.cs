using FluentResults;

namespace StampService.Application.Brands;

public interface IBrandCustomerService
{
    Task<Result<bool>> EnsureAsync(
        Guid brandId,
        Guid userId,
        Guid? createdByUserId,
        CancellationToken cancellationToken);
}
