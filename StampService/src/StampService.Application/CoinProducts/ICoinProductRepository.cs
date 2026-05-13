using StampService.Domain.Coins;

namespace StampService.Application.CoinProducts;

public interface ICoinProductRepository
{
    Task<CoinProduct?> GetByIdAsync(Guid productId, CancellationToken cancellationToken);

    Task<CoinProduct?> GetByIdForUpdateAsync(Guid productId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CoinProduct>> GetByBrandAsync(Guid brandId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CoinProduct>> GetActiveByBrandAsync(Guid brandId, CancellationToken cancellationToken);

    void Add(CoinProduct product);

    Task SaveAsync(CancellationToken cancellationToken);
}
