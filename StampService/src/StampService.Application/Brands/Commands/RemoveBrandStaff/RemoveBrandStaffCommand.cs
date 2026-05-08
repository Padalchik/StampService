using StampService.Application.Abstractions;

namespace StampService.Application.Brands.Commands.RemoveBrandStaff;

public record RemoveBrandStaffCommand(
    Guid ActorUserId,
    Guid BrandId,
    Guid StaffUserId) : ICommand;
