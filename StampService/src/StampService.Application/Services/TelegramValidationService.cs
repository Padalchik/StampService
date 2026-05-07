using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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

        var expectedHash = ComputeHash(request, _options.BotToken);

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

    private static string ComputeHash(TelegramLoginRequest request, string botToken)
    {
        var values = new SortedDictionary<string, string>
        {
            ["auth_date"] = request.AuthDate.ToString(CultureInfo.InvariantCulture),
            ["first_name"] = request.FirstName,
            ["id"] = request.Id.ToString(CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(request.LastName))
            values["last_name"] = request.LastName;

        if (!string.IsNullOrWhiteSpace(request.Username))
            values["username"] = request.Username;

        var dataCheckString = string.Join(
            "\n",
            values.Select(x => $"{x.Key}={x.Value}"));

        var secretKey = SHA256.HashData(Encoding.UTF8.GetBytes(botToken));
        var hash = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
