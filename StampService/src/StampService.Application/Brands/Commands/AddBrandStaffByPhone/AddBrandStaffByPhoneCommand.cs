using StampService.Application.Abstractions;

namespace StampService.Application.Brands.Commands.AddBrandStaffByPhone;

public record AddBrandStaffByPhoneCommand(
    Guid ActorUserId,
    Guid BrandId,
    string PhoneNumber) : ICommand;
