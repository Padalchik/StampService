using StampService.Application.Abstractions;

namespace StampService.Application.Wallet.Queries.GetUserWalletOverview;

public record GetUserWalletOverviewQuery(Guid UserId) : IQuery;
