using StampService.Domain.Brand;
using StampService.Domain.User;

namespace StampService.Application.Brands;

public interface IBrandCustomerRepository
{
    Task<BrandCustomer?> GetByBrandAndUserAsync(
        Guid brandId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<User?> GetCustomerByPhoneAsync(
        Guid brandId,
        IdentityType identityType,
        string identityKey,
        CancellationToken cancellationToken);

    void Add(BrandCustomer brandCustomer);

    Task SaveAsync(CancellationToken cancellationToken);
}
