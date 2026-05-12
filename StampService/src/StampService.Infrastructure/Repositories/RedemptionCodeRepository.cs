using Microsoft.EntityFrameworkCore;
using StampService.Application.Users;
using StampService.Domain.User;

namespace StampService.Infrastructure.Repositories;

public class RedemptionCodeRepository : IRedemptionCodeRepository
{
    private readonly AppDbContext _dbContext;

    public RedemptionCodeRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RedemptionCode?> GetActiveByUserIdAsync(
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        return await _dbContext.RedemptionCodes
            .Where(code => code.UserId == userId
                && code.Code.Length == RedemptionCode.CodeLength
                && code.UsedAtUtc == null
                && code.ExpiresAtUtc > nowUtc)
            .OrderByDescending(code => code.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<RedemptionCode?> GetActiveByCodeAsync(
        string code,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        return await _dbContext.RedemptionCodes
            .Where(item => item.Code == code
                && item.UsedAtUtc == null
                && item.ExpiresAtUtc > nowUtc)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ActiveCodeExistsAsync(
        string code,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        return await _dbContext.RedemptionCodes
            .AnyAsync(item => item.Code == code
                && item.UsedAtUtc == null
                && item.ExpiresAtUtc > nowUtc,
                cancellationToken);
    }

    public void Add(RedemptionCode redemptionCode)
    {
        _dbContext.RedemptionCodes.Add(redemptionCode);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
