using StampService.Application.Abstractions;
using StampService.Application.Administration;

namespace StampService.Application.Demo.Commands.CreateDemoBrands;

public record CreateDemoBrandsCommand(AdminActor Admin) : ICommand;
