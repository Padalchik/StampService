using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using StampService.Contracts.DTOs.Auth;

namespace StampService.Application.Services;

public class TelegramValidationService : ITelegramValidationService
{
    private readonly TelegramOptions _options;

    public TelegramValidationService(IOptions<TelegramOptions> options)
    {
        _options = options.Value;
    }

    public bool Validate(TelegramLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken))
            return false;

        if (string.IsNullOrWhiteSpace(request.Hash))
            return false;

        if (!IsAuthDateValid(request.AuthDate))
            return false;

        var expectedHash = TelegramAuthHashCalculator.ComputeHash(request, _options.BotToken);

        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(expectedHash),
                Convert.FromHexString(request.Hash));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private bool IsAuthDateValid(long authDate)
    {
        if (authDate <= 0)
            return false;

        if (_options.AuthDataMaxAgeMinutes <= 0)
            return false;

        var authDateTime = DateTimeOffset.FromUnixTimeSeconds(authDate);
        var maxAge = TimeSpan.FromMinutes(_options.AuthDataMaxAgeMinutes);

        return authDateTime <= DateTimeOffset.UtcNow
               && DateTimeOffset.UtcNow - authDateTime <= maxAge;
    }

}
