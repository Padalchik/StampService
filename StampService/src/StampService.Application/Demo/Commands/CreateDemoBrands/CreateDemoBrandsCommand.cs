using StampService.Application.Abstractions;

namespace StampService.Application.Demo.Commands.CreateDemoBrands;

public record CreateDemoBrandsCommand(long AdminTelegramUserId) : ICommand;
