using StampService.Application.Abstractions;

namespace StampService.Application.Metrics.Queries.GetMetricBalance;

public record GetMetricBalanceQuery(
    Guid MetricDefinitionId,
    Guid UserId,
    Guid RequestUserId) : IQuery;
