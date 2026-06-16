using StampService.Application.Abstractions;
using StampService.Contracts.DTOs.Brands;

namespace StampService.Application.Brands.Commands.CreateBrandCustomerByPhone;

public record CreateBrandCustomerByPhoneCommand(
    Guid ActorUserId,
    Guid BrandId,
    CreateBrandCustomerByPhoneRequest Request) : ICommand;
