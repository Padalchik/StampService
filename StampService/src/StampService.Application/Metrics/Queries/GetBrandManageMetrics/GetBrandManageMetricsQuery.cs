using StampService.Application.Abstractions;

namespace StampService.Application.Metrics.Queries.GetBrandManageMetrics;

public record GetBrandManageMetricsQuery(
    Guid UserId,
    Guid BrandId) : IQuery;
