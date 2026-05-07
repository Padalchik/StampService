using StampService.Application.Abstractions;

namespace StampService.Application.Metrics.Queries.GetBrandIssueMetrics;

public record GetBrandIssueMetricsQuery(
    Guid UserId,
    Guid BrandId) : IQuery;
