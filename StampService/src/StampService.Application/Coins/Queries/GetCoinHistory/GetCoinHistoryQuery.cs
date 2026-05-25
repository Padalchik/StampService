using StampService.Application.Abstractions;

namespace StampService.Application.Coins.Queries.GetCoinHistory;

public record GetCoinHistoryQuery(
    Guid BrandId,
    Guid RequestUserId,
    string CustomerPhoneNumber,
    int Skip,
    int Take) : IQuery;
