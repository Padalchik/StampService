using Microsoft.EntityFrameworkCore;
using StampService.Application.Coins;
using StampService.Domain.Coins;

namespace StampService.Infrastructure.Repositories;

public class CoinWalletRepository : ICoinWalletRepository
{
    private readonly AppDbContext _dbContext;

    public CoinWalletRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CoinWallet?> GetByUserAndBrandAsync(
        Guid userId,
        Guid brandId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CoinWallets
            .FirstOrDefaultAsync(
                wallet => wallet.UserId == userId && wallet.BrandId == brandId,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserCoinWalletReadModel>> GetUserWalletsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CoinWallets
            .AsNoTracking()
            .Where(wallet => wallet.UserId == userId)
            .Select(wallet => new UserCoinWalletReadModel(
                wallet.Id,
                wallet.BrandId,
                wallet.Brand.Name,
                wallet.Value))
            .ToArrayAsync(cancellationToken);
    }

    public void Add(CoinWallet wallet)
    {
        _dbContext.CoinWallets.Add(wallet);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
