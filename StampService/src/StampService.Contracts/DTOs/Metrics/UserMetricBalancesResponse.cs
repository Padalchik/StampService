namespace StampService.Contracts.DTOs.Metrics;

public record UserMetricBalancesResponse(
    Guid UserId,
    IReadOnlyCollection<UserMetricBalanceResponse> Balances);
