namespace StampService.Contracts.DTOs.Metrics;

public record BrandCustomerMetricBalancesResponse(
    Guid BrandId,
    Guid CustomerUserId,
    string CustomerName,
    string CustomerPhoneNumber,
    int CoinBalanceValue,
    IReadOnlyCollection<BrandCustomerMetricBalanceResponse> Balances);
