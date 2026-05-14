namespace StampService.TelegramBot.Features.Admin.Actions;

public record OpenAdminBrandPayload(
    Guid BrandId,
    string BrandName,
    bool IsMetricsEnabled,
    bool IsCoinsEnabled,
    Guid? OwnerUserId,
    string? OwnerName,
    string? OwnerCustomerCode);
