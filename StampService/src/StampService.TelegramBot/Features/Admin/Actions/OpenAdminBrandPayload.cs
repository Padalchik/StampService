namespace StampService.TelegramBot.Features.Admin.Actions;

public record OpenAdminBrandPayload(
    Guid BrandId,
    string BrandName,
    bool IsMetricsEnabled,
    bool IsCoinsEnabled,
    bool IsCoinProductRedemptionEnabled,
    bool IsManualCoinRedemptionEnabled,
    Guid? OwnerUserId,
    string? OwnerName,
    string? OwnerPhoneNumber);
