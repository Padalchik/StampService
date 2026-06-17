using Microsoft.EntityFrameworkCore;
using StampService.Application.Brands;
using StampService.Domain.Brand;
using StampService.Domain.User;

namespace StampService.Infrastructure.Repositories;

public class BrandCustomerRepository : IBrandCustomerRepository
{
    private readonly AppDbContext _dbContext;

    public BrandCustomerRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BrandCustomer?> GetByBrandAndUserAsync(
        Guid brandId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.BrandCustomers
            .FirstOrDefaultAsync(
                customer => customer.BrandId == brandId && customer.UserId == userId,
                cancellationToken);
    }

    public async Task<User?> GetCustomerByPhoneAsync(
        Guid brandId,
        IdentityType identityType,
        string identityKey,
        CancellationToken cancellationToken)
    {
        var brandCustomer = await _dbContext.BrandCustomers
            .Include(customer => customer.User)
            .ThenInclude(user => user.Identities)
            .Where(customer => customer.BrandId == brandId)
            .Where(customer => customer.User.Identities.Any(identity =>
                identity.DeletedAt == null
                && identity.Type == identityType
                && identity.Key == identityKey))
            .FirstOrDefaultAsync(cancellationToken);

        return brandCustomer?.User;
    }

    public void Add(BrandCustomer brandCustomer)
    {
        _dbContext.BrandCustomers.Add(brandCustomer);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
