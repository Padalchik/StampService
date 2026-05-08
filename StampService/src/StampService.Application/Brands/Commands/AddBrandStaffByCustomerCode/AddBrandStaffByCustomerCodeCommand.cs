using StampService.Application.Abstractions;

namespace StampService.Application.Brands.Commands.AddBrandStaffByCustomerCode;

public record AddBrandStaffByCustomerCodeCommand(
    Guid ActorUserId,
    Guid BrandId,
    string CustomerCode) : ICommand;
