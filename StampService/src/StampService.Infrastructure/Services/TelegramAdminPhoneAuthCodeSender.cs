using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FluentResults;
using Microsoft.Extensions.Configuration;
using StampService.Application.Auth;
using StampService.Application.Errors;

namespace StampService.Infrastructure.Services;

public class TelegramAdminPhoneAuthCodeSender : IPhoneAuthCodeSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public TelegramAdminPhoneAuthCodeSender(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<Result> SendAsync(
        string phoneNumber,
        string code,
        bool sendSms,
        CancellationToken cancellationToken)
    {
        var botToken = _configuration["Telegram:BotToken"];
        if (string.IsNullOrWhiteSpace(botToken))
            return Result.Fail(AuthErrors.PhoneCodeSendFailed("Telegram:BotToken is not configured"));

        var adminIds = _configuration
            .GetSection("Admin:TelegramUserIds")
            .GetChildren()
            .Select(item => long.TryParse(item.Value, out var value) ? value : 0)
            .Where(value => value > 0)
            .Distinct()
            .ToArray();
        if (adminIds.Length == 0)
            return Result.Fail(AuthErrors.PhoneCodeSendFailed("Admin:TelegramUserIds is not configured"));

        var text = $"Код авторизации для {phoneNumber}: {code}";
        foreach (var adminId in adminIds)
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"https://api.telegram.org/bot{botToken}/sendMessage",
                new SendMessageRequest(adminId, text),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Result.Fail(AuthErrors.PhoneCodeSendFailed(
                    $"Telegram sendMessage failed with status {(int)response.StatusCode}"));
        }

        return Result.Ok();
    }

    private sealed record SendMessageRequest(
        [property: JsonPropertyName("chat_id")] long ChatId,
        [property: JsonPropertyName("text")] string Text);
}
