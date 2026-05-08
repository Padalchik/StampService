using StampService.Application.Abstractions;

namespace StampService.Application.Brands.Commands.CreateBrandWithOwner;

public record CreateBrandWithOwnerCommand(
    long AdminTelegramUserId,
    string BrandName,
    string OwnerCustomerCode) : ICommand;
