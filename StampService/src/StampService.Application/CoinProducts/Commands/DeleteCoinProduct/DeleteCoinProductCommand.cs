using StampService.Application.Abstractions;

namespace StampService.Application.CoinProducts.Commands.DeleteCoinProduct;

public record DeleteCoinProductCommand(
    Guid ProductId,
    Guid RequestUserId) : ICommand;
