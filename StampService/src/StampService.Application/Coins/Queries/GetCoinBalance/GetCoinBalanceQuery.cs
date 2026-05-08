using StampService.Application.Abstractions;

namespace StampService.Application.Coins.Queries.GetCoinBalance;

public record GetCoinBalanceQuery(
    Guid BrandId,
    Guid RequestUserId,
    string CustomerCode) : IQuery;
