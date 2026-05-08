using StampService.Contracts.DTOs.Coins;

namespace StampService.Contracts.DTOs.Metrics;

public record UserMetricBalancesResponse(
    Guid UserId,
    IReadOnlyCollection<UserMetricBalanceResponse> Balances,
    IReadOnlyCollection<UserCoinWalletResponse> CoinWallets);
