using StampService.Application.Abstractions;
using StampService.Contracts.DTOs.CoinProducts;

namespace StampService.Application.CoinProducts.Commands.UpdateCoinProduct;

public record UpdateCoinProductCommand(
    Guid ProductId,
    Guid RequestUserId,
    UpdateCoinProductRequest Request) : ICommand;
