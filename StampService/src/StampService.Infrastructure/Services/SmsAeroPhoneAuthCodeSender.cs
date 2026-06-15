using FluentResults;
using Microsoft.Extensions.Options;
using SmsAero;
using StampService.Application.Auth;
using StampService.Application.Errors;

namespace StampService.Infrastructure.Services;

public sealed class SmsAeroPhoneAuthCodeSender : IPhoneAuthCodeSender
{
    private const string MessageTemplate = "Код авторизации: {0}";

    private readonly IPhoneAuthSmsSettingsRepository _settingsRepository;
    private readonly IOptions<SmsAeroOptions> _options;

    public SmsAeroPhoneAuthCodeSender(
        IPhoneAuthSmsSettingsRepository settingsRepository,
        IOptions<SmsAeroOptions> options)
    {
        _settingsRepository = settingsRepository;
        _options = options;
    }

    public async Task<Result> SendAsync(
        string phoneNumber,
        string code,
        bool sendSms,
        CancellationToken cancellationToken)
    {
        if (!sendSms)
            return Result.Ok();

        var settings = await _settingsRepository.GetOrCreateAsync(cancellationToken);
        if (!settings.IsEnabled)
            return Result.Fail(AuthErrors.PhoneSmsDisabled());

        var login = _options.Value.Login;
        if (string.IsNullOrWhiteSpace(login))
            return Result.Fail(AuthErrors.PhoneCodeSendFailed("SmsAero:Login is not configured"));

        var apiKey = _options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            return Result.Fail(AuthErrors.PhoneCodeSendFailed("SmsAero:ApiKey is not configured"));

        try
        {
            var client = new SmsAeroClient(login, apiKey);
            await client.SmsSend(
                string.Format(MessageTemplate, code),
                FormatPhoneForSmsAero(phoneNumber));
        }
        catch (Exception ex)
        {
            return Result.Fail(AuthErrors.PhoneCodeSendFailed($"SmsAero send failed: {ex.Message}"));
        }

        return Result.Ok();
    }

    private static string FormatPhoneForSmsAero(string phoneNumber)
    {
        return phoneNumber.StartsWith('+')
            ? phoneNumber[1..]
            : phoneNumber;
    }
}
