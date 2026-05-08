using StampService.Application.Users;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Fakes;

public class FakeRedemptionCodeRepository : IRedemptionCodeRepository
{
    public List<RedemptionCode> Codes { get; } = [];
    public int SaveCount { get; private set; }

    public Task<RedemptionCode?> GetActiveByUserIdAsync(
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var code = Codes
            .Where(code => code.UserId == userId && code.IsActive(nowUtc))
            .OrderByDescending(code => code.CreatedAt)
            .FirstOrDefault();

        return Task.FromResult(code);
    }

    public Task<RedemptionCode?> GetActiveByCodeAsync(
        string code,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var redemptionCode = Codes
            .Where(item => item.Code == code && item.IsActive(nowUtc))
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefault();

        return Task.FromResult(redemptionCode);
    }

    public Task<bool> ActiveCodeExistsAsync(
        string code,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Codes.Any(item => item.Code == code && item.IsActive(nowUtc)));
    }

    public void Add(RedemptionCode redemptionCode)
    {
        Codes.Add(redemptionCode);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
