using StampService.Application.Abstractions;

namespace StampService.Application.Metrics.Queries.GetBrandCustomerMetricBalances;

public record GetBrandCustomerMetricBalancesQuery(
    Guid RequestUserId,
    Guid BrandId,
    string CustomerPhoneNumber) : IQuery;
