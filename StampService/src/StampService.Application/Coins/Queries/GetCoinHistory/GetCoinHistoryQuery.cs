using StampService.Application.Abstractions;

namespace StampService.Application.Coins.Queries.GetCoinHistory;

public record GetCoinHistoryQuery(
    Guid BrandId,
    Guid RequestUserId,
    string CustomerCode,
    int Skip,
    int Take) : IQuery;
