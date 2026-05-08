using StampService.Domain.Coins;

namespace StampService.Application.Coins;

public interface ICoinWalletRepository
{
    Task<CoinWallet?> GetByUserAndBrandAsync(
        Guid userId,
        Guid brandId,
        CancellationToken cancellationToken);

    void Add(CoinWallet wallet);

    Task SaveAsync(CancellationToken cancellationToken);
}
