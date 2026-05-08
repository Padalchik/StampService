using StampService.Application.Abstractions;

namespace StampService.Application.Metrics.Queries.GetBrandRedeemMetrics;

public record GetBrandRedeemMetricsQuery(
    Guid UserId,
    Guid BrandId) : IQuery;
