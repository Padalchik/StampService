using StampService.Domain.Coins;

namespace StampService.Application.Coins;

public interface ICoinTransactionRepository
{
    Task<IReadOnlyCollection<CoinTransaction>> GetHistoryByWalletAsync(
        Guid coinWalletId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<int> CalculateWalletValueAsync(
        Guid coinWalletId,
        CancellationToken cancellationToken);

    void Add(CoinTransaction transaction);

    Task SaveAsync(CancellationToken cancellationToken);
}
