using StampService.Application.Abstractions;

namespace StampService.Application.Wallet.Queries.GetUserBrandWalletHistory;

public record GetUserBrandWalletHistoryQuery(
    Guid UserId,
    Guid BrandId,
    int Skip,
    int Take) : IQuery;
