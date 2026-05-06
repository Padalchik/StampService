using StampService.Application.Abstractions;

namespace StampService.Application.Metrics.Queries.GetMetricTransactions;

public record GetMetricTransactionsQuery(
    Guid MetricDefinitionId,
    Guid UserId,
    Guid RequestUserId,
    int Skip,
    int Take) : IQuery;
