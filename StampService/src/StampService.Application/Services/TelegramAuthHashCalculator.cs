using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using StampService.Contracts.DTOs.Auth;

namespace StampService.Application.Services;

public static class TelegramAuthHashCalculator
{
    public static string ComputeHash(TelegramLoginRequest request, string botToken)
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
