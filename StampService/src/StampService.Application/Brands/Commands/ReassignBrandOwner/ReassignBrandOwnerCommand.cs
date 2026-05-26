using StampService.Application.Abstractions;
using StampService.Application.Administration;

namespace StampService.Application.Brands.Commands.ReassignBrandOwner;

public record ReassignBrandOwnerCommand(
    AdminActor Admin,
    Guid BrandId,
    string NewOwnerPhoneNumber) : ICommand;
