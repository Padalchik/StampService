using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Metrics;

namespace StampService.Application.Metrics.Queries.GetUserMetricBalances;

public class GetUserMetricBalancesHandler
    : IQueryHandler<UserMetricBalancesResponse, GetUserMetricBalancesQuery>
{
    private readonly IMetricBalanceRepository _metricBalanceRepository;
    private readonly IUserRepository _userRepository;

    public GetUserMetricBalancesHandler(
        IMetricBalanceRepository metricBalanceRepository,
        IUserRepository userRepository)
    {
        _metricBalanceRepository = metricBalanceRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<UserMetricBalancesResponse>> Handle(
        GetUserMetricBalancesQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        var userExists = await _userRepository.ExistsAsync(query.UserId, cancellationToken);
        if (!userExists)
            return Result.Fail(UserErrors.NotFound());

        var balances = await _metricBalanceRepository.GetUserBalancesAsync(
            query.UserId,
            cancellationToken);

        var response = new UserMetricBalancesResponse(
            query.UserId,
            balances
                .OrderBy(balance => balance.BrandName)
                .ThenBy(balance => balance.MetricName)
                .Select(balance => new UserMetricBalanceResponse(
                    balance.BalanceId,
                    balance.BrandId,
                    balance.BrandName,
                    balance.MetricDefinitionId,
                    balance.MetricCode,
                    balance.MetricName,
                    balance.Value))
                .ToArray());

        return Result.Ok(response);
    }
}
