using StampService.Application.Abstractions;

namespace StampService.Application.Metrics.Queries.GetUserMetricTransactions;

public record GetUserMetricTransactionsQuery(
    Guid MetricDefinitionId,
    Guid UserId,
    int Skip,
    int Take) : IQuery;
