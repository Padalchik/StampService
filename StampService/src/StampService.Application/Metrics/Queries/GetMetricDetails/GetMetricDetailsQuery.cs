using StampService.Application.Abstractions;

namespace StampService.Application.Metrics.Queries.GetMetricDetails;

public record GetMetricDetailsQuery(
    Guid UserId,
    Guid MetricDefinitionId) : IQuery;
