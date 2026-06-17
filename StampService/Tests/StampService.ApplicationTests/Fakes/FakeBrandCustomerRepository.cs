using StampService.Application.Brands;
using StampService.Domain.Brand;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Fakes;

public class FakeBrandCustomerRepository : IBrandCustomerRepository
{
    private readonly List<BrandCustomer> _customers = [];
    private readonly FakeUserRepository _userRepository;

    public FakeBrandCustomerRepository(FakeUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public IReadOnlyCollection<BrandCustomer> Customers => _customers;

    public Task<BrandCustomer?> GetByBrandAndUserAsync(
        Guid brandId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_customers.FirstOrDefault(customer =>
            customer.BrandId == brandId && customer.UserId == userId));
    }

    public async Task<User?> GetCustomerByPhoneAsync(
        Guid brandId,
        IdentityType identityType,
        string identityKey,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdentityAsync(
            identityType,
            identityKey,
            cancellationToken);
        if (user is null)
            return null;

        return _customers.Any(customer => customer.BrandId == brandId && customer.UserId == user.Id)
            ? user
            : null;
    }

    public Task<IReadOnlyCollection<UserBrandCustomerReadModel>> GetUserBrandCustomersAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<UserBrandCustomerReadModel> result = _customers
            .Where(customer => customer.UserId == userId)
            .Select(customer => new UserBrandCustomerReadModel(customer.BrandId))
            .ToArray();

        return Task.FromResult(result);
    }

    public void Add(BrandCustomer brandCustomer)
    {
        _customers.Add(brandCustomer);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return _userRepository.SaveAsync(cancellationToken);
    }

    public void AddExisting(Guid brandId, Guid userId, Guid? createdByUserId = null)
    {
        Add(BrandCustomer.Create(brandId, userId, createdByUserId).Value);
    }
}
