using StampService.Application.Abstractions;
using StampService.Contracts.DTOs.CoinProducts;

namespace StampService.Application.CoinProducts.Commands.CreateCoinProduct;

public record CreateCoinProductCommand(
    Guid BrandId,
    Guid RequestUserId,
    CreateCoinProductRequest Request) : ICommand;
