using StampService.Application.Abstractions;
using StampService.Contracts.DTOs.Brands;

namespace StampService.Application.Brands.Commands.AddBrandStaff;

public record AddBrandStaffCommand(
    Guid BrandId,
    Guid RequestUserId,
    AddBrandStaffRequest Request) : ICommand;
