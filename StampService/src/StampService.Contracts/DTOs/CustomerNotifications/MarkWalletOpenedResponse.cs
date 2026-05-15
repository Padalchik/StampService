namespace StampService.Contracts.DTOs.CustomerNotifications;

public record MarkWalletOpenedResponse(Guid UserId, DateTime LastWalletOpenedAtUtc);
