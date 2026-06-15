using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StampService.Application.Auth;
using StampService.Domain.User;
using StampService.Infrastructure.Services;

namespace StampService.Infrastructure.Repositories;

public class PhoneAuthSmsSettingsRepository : IPhoneAuthSmsSettingsRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IOptions<SmsAeroOptions> _fallbackOptions;

    public PhoneAuthSmsSettingsRepository(
        AppDbContext dbContext,
        IOptions<SmsAeroOptions> fallbackOptions)
    {
        _dbContext = dbContext;
        _fallbackOptions = fallbackOptions;
    }

    public async Task<PhoneAuthSmsSettings> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.PhoneAuthSmsSettings
            .FirstOrDefaultAsync(item => item.Id == PhoneAuthSmsSettings.SingletonId, cancellationToken);
        if (settings is not null)
            return settings;

        settings = PhoneAuthSmsSettings.Create(_fallbackOptions.Value.SendAuthCodes);
        _dbContext.PhoneAuthSmsSettings.Add(settings);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return settings;
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
