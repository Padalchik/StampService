namespace StampService.Infrastructure.Services;

public sealed class SmsAeroOptions
{
    public string? Login { get; set; }
    public string? ApiKey { get; set; }
    public bool SendAuthCodes { get; set; } = true;
}
