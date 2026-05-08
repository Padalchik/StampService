namespace StampService.TelegramBot.Features.Staff.Actions;

public record OpenBrandStaffPayload(
    Guid BrandId,
    string BrandName);
