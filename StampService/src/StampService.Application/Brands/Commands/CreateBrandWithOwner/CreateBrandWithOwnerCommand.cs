using StampService.Application.Abstractions;
using StampService.Application.Administration;

namespace StampService.Application.Brands.Commands.CreateBrandWithOwner;

public record CreateBrandWithOwnerCommand(
    AdminActor Admin,
    string BrandName,
    string OwnerPhoneNumber) : ICommand;
