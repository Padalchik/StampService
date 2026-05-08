using StampService.Domain.Coins;

namespace StampService.Application.Coins;

public interface ICoinWalletRepository
{
    Task<CoinWallet?> GetByUserAndBrandAsync(
        Guid userId,
        Guid brandId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UserCoinWalletReadModel>> GetUserWalletsAsync(
        Guid userId,
        CancellationToken cancellationToken);

    void Add(CoinWallet wallet);

    Task SaveAsync(CancellationToken cancellationToken);
}
