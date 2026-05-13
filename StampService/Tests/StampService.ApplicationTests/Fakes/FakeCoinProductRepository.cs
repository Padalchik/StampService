using StampService.Application.CoinProducts;
using StampService.Domain.Coins;

namespace StampService.ApplicationTests.Fakes;

public class FakeCoinProductRepository : ICoinProductRepository
{
    private readonly List<CoinProduct> _products = [];

    public IReadOnlyCollection<CoinProduct> Products => _products;

    public int SaveCount { get; private set; }

    public Task<CoinProduct?> GetByIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_products.FirstOrDefault(product => product.Id == productId));
    }

    public Task<CoinProduct?> GetByIdForUpdateAsync(Guid productId, CancellationToken cancellationToken)
    {
        return GetByIdAsync(productId, cancellationToken);
    }

    public Task<IReadOnlyCollection<CoinProduct>> GetByBrandAsync(
        Guid brandId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<CoinProduct> products = _products
            .Where(product => product.BrandId == brandId)
            .ToArray();

        return Task.FromResult(products);
    }

    public Task<IReadOnlyCollection<CoinProduct>> GetActiveByBrandAsync(
        Guid brandId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<CoinProduct> products = _products
            .Where(product => product.BrandId == brandId && product.IsActive)
            .ToArray();

        return Task.FromResult(products);
    }

    public void Add(CoinProduct product)
    {
        _products.Add(product);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
