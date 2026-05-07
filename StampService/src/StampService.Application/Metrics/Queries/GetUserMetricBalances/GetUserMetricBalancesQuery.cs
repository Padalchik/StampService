using StampService.Application.Abstractions;

namespace StampService.Application.Metrics.Queries.GetUserMetricBalances;

public record GetUserMetricBalancesQuery(Guid UserId) : IQuery;
