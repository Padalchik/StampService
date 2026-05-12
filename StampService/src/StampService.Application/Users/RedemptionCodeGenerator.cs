using System.Security.Cryptography;
using StampService.Domain.User;

namespace StampService.Application.Users;

public class RedemptionCodeGenerator : IRedemptionCodeGenerator
{
    private const int RandomAttempts = 100;

    private readonly IRedemptionCodeRepository _redemptionCodeRepository;

    public RedemptionCodeGenerator(IRedemptionCodeRepository redemptionCodeRepository)
    {
        _redemptionCodeRepository = redemptionCodeRepository;
    }

    public async Task<string> GenerateAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var combinationsCount = (int)Math.Pow(10, RedemptionCode.CodeLength);
        for (var attempt = 0; attempt < RandomAttempts; attempt++)
        {
            var code = RandomNumberGenerator
                .GetInt32(0, combinationsCount)
                .ToString($"D{RedemptionCode.CodeLength}");

            var exists = await _redemptionCodeRepository.ActiveCodeExistsAsync(
                code,
                nowUtc,
                cancellationToken);

            if (!exists)
                return code;
        }

        var start = RandomNumberGenerator.GetInt32(0, combinationsCount);
        for (var offset = 0; offset < combinationsCount; offset++)
        {
            var value = (start + offset) % combinationsCount;
            var code = value.ToString($"D{RedemptionCode.CodeLength}");
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
