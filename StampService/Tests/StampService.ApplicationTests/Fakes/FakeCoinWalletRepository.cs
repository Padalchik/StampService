using StampService.Application.Coins;
using StampService.Domain.Coins;

namespace StampService.ApplicationTests.Fakes;

public class FakeCoinWalletRepository : ICoinWalletRepository
{
    private readonly List<CoinWallet> _wallets = [];

    public IReadOnlyCollection<CoinWallet> Wallets => _wallets;

    public int SaveCount { get; private set; }

    public Task<CoinWallet?> GetByUserAndBrandAsync(
        Guid userId,
        Guid brandId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_wallets.FirstOrDefault(wallet =>
            wallet.UserId == userId
            && wallet.BrandId == brandId));
    }

    public Task<IReadOnlyCollection<UserCoinWalletReadModel>> GetUserWalletsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<UserCoinWalletReadModel> wallets = _wallets
            .Where(wallet => wallet.UserId == userId)
            .Select(wallet => new UserCoinWalletReadModel(
                wallet.Id,
                wallet.BrandId,
                $"Brand {wallet.BrandId:N}",
                wallet.Value))
            .ToArray();

        return Task.FromResult(wallets);
    }

    public void Add(CoinWallet wallet)
    {
        _wallets.Add(wallet);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
