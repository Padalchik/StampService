using StampService.Application.Abstractions;

namespace StampService.Application.Brands.Commands.ReassignBrandOwner;

public record ReassignBrandOwnerCommand(
    long AdminTelegramUserId,
    Guid BrandId,
    string NewOwnerPhoneNumber) : ICommand;
