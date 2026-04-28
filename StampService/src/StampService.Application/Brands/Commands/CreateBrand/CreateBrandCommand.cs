using StampService.Application.Abstractions;
using StampService.Contracts.DTOs.Brands;

namespace StampService.Application.Brands.Commands.CreateBrand;

public record CreateBrandCommand(CreateBrandRequest Request, Guid UserId) : ICommand;
