using StampService.Application.Abstractions;

namespace StampService.Application.CoinProducts.Queries.GetBrandCoinProducts;

public record GetBrandCoinProductsQuery(
    Guid RequestUserId,
    Guid BrandId) : IQuery;
