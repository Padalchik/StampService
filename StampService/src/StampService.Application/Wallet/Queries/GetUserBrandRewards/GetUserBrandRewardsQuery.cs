using StampService.Application.Abstractions;

namespace StampService.Application.Wallet.Queries.GetUserBrandRewards;

public record GetUserBrandRewardsQuery(
    Guid UserId,
    Guid BrandId) : IQuery;
