using StampService.Domain.User;

namespace StampService.Application.Users;

public interface IRedemptionCodeRepository
{
    Task<RedemptionCode?> GetActiveByUserIdAsync(
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    Task<RedemptionCode?> GetActiveByCodeAsync(
        string code,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    Task<bool> ActiveCodeExistsAsync(
        string code,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    void Add(RedemptionCode redemptionCode);

    Task SaveAsync(CancellationToken cancellationToken);
}
