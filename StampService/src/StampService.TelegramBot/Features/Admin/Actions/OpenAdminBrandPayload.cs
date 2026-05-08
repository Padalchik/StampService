namespace StampService.TelegramBot.Features.Admin.Actions;

public record OpenAdminBrandPayload(
    Guid BrandId,
    string BrandName,
    Guid? OwnerUserId,
    string? OwnerName,
    string? OwnerCustomerCode);
