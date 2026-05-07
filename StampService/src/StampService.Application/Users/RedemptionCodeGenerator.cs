using System.Security.Cryptography;
using StampService.Domain.User;

namespace StampService.Application.Users;

public class RedemptionCodeGenerator : IRedemptionCodeGenerator
{
    private const int MaxAttempts = 25;

    private readonly IRedemptionCodeRepository _redemptionCodeRepository;

    public RedemptionCodeGenerator(IRedemptionCodeRepository redemptionCodeRepository)
    {
        _redemptionCodeRepository = redemptionCodeRepository;
    }

    public async Task<string> GenerateAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var code = RandomNumberGenerator
                .GetInt32(0, 1_000_000)
                .ToString($"D{RedemptionCode.CodeLength}");

            var exists = await _redemptionCodeRepository.ActiveCodeExistsAsync(
                code,
                nowUtc,
                cancellationToken);

            if (!exists)
                return code;
        }

        throw new InvalidOperationException("Could not generate unique redemption code");
    }
}
