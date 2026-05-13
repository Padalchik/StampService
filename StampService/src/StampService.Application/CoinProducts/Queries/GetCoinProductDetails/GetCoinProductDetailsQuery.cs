using StampService.Application.Abstractions;

namespace StampService.Application.CoinProducts.Queries.GetCoinProductDetails;

public record GetCoinProductDetailsQuery(
    Guid RequestUserId,
    Guid ProductId) : IQuery;
