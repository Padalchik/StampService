using FluentResults;

namespace StampService.Application.Brands;

public class BrandCustomerService : IBrandCustomerService
{
    private readonly IBrandCustomerRepository _brandCustomerRepository;

    public BrandCustomerService(IBrandCustomerRepository brandCustomerRepository)
    {
        _brandCustomerRepository = brandCustomerRepository;
    }

    public async Task<Result<bool>> EnsureAsync(
        Guid brandId,
        Guid userId,
        Guid? createdByUserId,
        CancellationToken cancellationToken)
    {
        var existingCustomer = await _brandCustomerRepository.GetByBrandAndUserAsync(
            brandId,
            userId,
            cancellationToken);
        if (existingCustomer is not null)
            return Result.Ok(false);

        var customerResult = Domain.Brand.BrandCustomer.Create(
            brandId,
            userId,
            createdByUserId);
        if (customerResult.IsFailed)
            return Result.Fail<bool>(customerResult.Errors);

        _brandCustomerRepository.Add(customerResult.Value);
        return Result.Ok(true);
    }
}
