using StampService.Application.Abstractions;

namespace StampService.Application.Wallet.Queries.GetUserWalletBrandDetails;

public record GetUserWalletBrandDetailsQuery(
    Guid UserId,
    Guid BrandId) : IQuery;

