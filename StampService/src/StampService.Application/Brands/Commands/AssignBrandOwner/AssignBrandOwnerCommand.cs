using StampService.Application.Abstractions;
using StampService.Contracts.DTOs.Brands;

namespace StampService.Application.Brands.Commands.AssignBrandOwner;

public record AssignBrandOwnerCommand(
    Guid BrandId,
    AssignBrandOwnerRequest Request) : ICommand;
