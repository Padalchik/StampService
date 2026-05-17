using Microsoft.EntityFrameworkCore;
using StampService.Application.Auth;
using StampService.Domain.User;

namespace StampService.Infrastructure.Repositories;

public class PhoneAuthCodeRepository : IPhoneAuthCodeRepository
{
    private readonly AppDbContext _dbContext;

    public PhoneAuthCodeRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<PhoneAuthCode>> GetActiveByPhoneAsync(
        string phoneNumber,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        return await _dbContext.PhoneAuthCodes
            .Where(code => code.PhoneNumber == phoneNumber
                && code.UsedAtUtc == null
                && code.ExpiresAtUtc > nowUtc
                && code.FailedAttempts < PhoneAuthCode.MaxAttempts)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<PhoneAuthCode?> GetLatestActiveByPhoneAsync(
        string phoneNumber,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        return await _dbContext.PhoneAuthCodes
            .Where(code => code.PhoneNumber == phoneNumber
                && code.UsedAtUtc == null
                && code.ExpiresAtUtc > nowUtc
                && code.FailedAttempts < PhoneAuthCode.MaxAttempts)
            .OrderByDescending(code => code.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Add(PhoneAuthCode code)
    {
        _dbContext.PhoneAuthCodes.Add(code);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
