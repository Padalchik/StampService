using StampService.Application.Abstractions;

namespace StampService.Application.CoinProducts.Commands.PurchaseCoinProduct;

public record PurchaseCoinProductCommand(
    Guid BrandId,
    Guid RequestUserId,
    string RedemptionCode,
    Guid ProductId) : ICommand;
