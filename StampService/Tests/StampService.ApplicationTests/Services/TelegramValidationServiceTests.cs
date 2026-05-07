using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using StampService.Application.Services;
using StampService.Contracts.DTOs.Auth;

namespace StampService.ApplicationTests.Services;

public class TelegramValidationServiceTests
{
    private const string BotToken = "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11";

    [Fact]
    public void Validate_WhenTelegramHashIsValid_ShouldReturnTrue()
    {
        var request = CreateRequest();
        var service = CreateService();

        var isValid = service.Validate(request);

        Assert.True(isValid);
    }

    [Fact]
    public void Validate_WhenHashIsInvalid_ShouldReturnFalse()
    {
        var request = CreateRequest(hash: "not-a-valid-hash");
        var service = CreateService();

        var isValid = service.Validate(request);

        Assert.False(isValid);
    }

    [Fact]
    public void Validate_WhenAuthDateIsExpired_ShouldReturnFalse()
    {
        var authDate = DateTimeOffset.UtcNow.AddDays(-2).ToUnixTimeSeconds();
        var request = CreateRequest(authDate: authDate);
        var service = CreateService(maxAgeMinutes: 60);

        var isValid = service.Validate(request);

        Assert.False(isValid);
    }

    [Fact]
    public void Validate_WhenBotTokenIsMissing_ShouldReturnFalse()
    {
        var request = CreateRequest();
        var service = CreateService(botToken: string.Empty);

        var isValid = service.Validate(request);

        Assert.False(isValid);
    }

    private static TelegramValidationService CreateService(
        string botToken = BotToken,
        int maxAgeMinutes = 1440)
    {
        var options = Options.Create(new TelegramOptions
        {
            BotToken = botToken,
            AuthDataMaxAgeMinutes = maxAgeMinutes
        });

        return new TelegramValidationService(options);
    }

    private static TelegramLoginRequest CreateRequest(
        string? hash = null,
        long? authDate = null)
    {
        var request = new TelegramLoginRequest(
            Id: 123456789,
            FirstName: "Ivan",
            LastName: "Petrov",
            Username: "ivan",
            Hash: string.Empty,
            AuthDate: authDate ?? DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds());

        return request with
        {
            Hash = hash ?? ComputeHash(request)
        };
    }

    private static string ComputeHash(TelegramLoginRequest request)
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

        var secretKey = SHA256.HashData(Encoding.UTF8.GetBytes(BotToken));
        var hash = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
