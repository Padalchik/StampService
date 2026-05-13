using StampService.Application.Abstractions;

namespace StampService.Application.CoinProducts.Queries.GetCoinProductPurchaseOptions;

public record GetCoinProductPurchaseOptionsQuery(
    Guid RequestUserId,
    Guid BrandId,
    string RedemptionCode) : IQuery;
