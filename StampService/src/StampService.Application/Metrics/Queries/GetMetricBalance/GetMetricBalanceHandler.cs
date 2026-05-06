using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Access;

namespace StampService.Application.Metrics.Queries.GetMetricBalance;

public class GetMetricBalanceHandler : IQueryHandler<MetricBalanceResponse, GetMetricBalanceQuery>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly ILoyaltyMetricRepository _metricRepository;
    private readonly IMetricBalanceRepository _metricBalanceRepository;
    private readonly IUserRepository _userRepository;

    public GetMetricBalanceHandler(
        IBrandAccessService brandAccessService,
        ILoyaltyMetricRepository metricRepository,
        IMetricBalanceRepository metricBalanceRepository,
        IUserRepository userRepository)
    {
        _brandAccessService = brandAccessService;
        _metricRepository = metricRepository;
        _metricBalanceRepository = metricBalanceRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<MetricBalanceResponse>> Handle(
        GetMetricBalanceQuery query,
        CancellationToken cancellationToken)
    {
        var metric = await _metricRepository.GetByIdAsync(
            query.MetricDefinitionId,
            cancellationToken);

        if (metric is null)
            return Result.Fail("Metric not found");

        var canViewBalance = await _brandAccessService.CanAsync(
            query.RequestUserId,
            metric.BrandId,
            PermissionCode.BalanceView,
            cancellationToken);

        if (!canViewBalance)
            return Result.Fail("Access denied");

        var userExists = await _userRepository.ExistsAsync(query.UserId, cancellationToken);
        if (!userExists)
            return Result.Fail("User not found");

        var balance = await _metricBalanceRepository.GetByUserAndMetricAsync(
            query.UserId,
            metric.BrandId,
            query.MetricDefinitionId,
            cancellationToken);

        var response = new MetricBalanceResponse(
            balance?.Id,
            metric.BrandId,
            query.MetricDefinitionId,
            query.UserId,
            balance?.Value ?? 0);

        return Result.Ok(response);
    }
}
