using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Access;

namespace StampService.Application.Metrics.Queries.GetBrandRedeemMetrics;

public class GetBrandRedeemMetricsHandler : IQueryHandler<IReadOnlyCollection<MetricResponse>, GetBrandRedeemMetricsQuery>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly ILoyaltyMetricRepository _metricRepository;
    private readonly IUserRepository _userRepository;

    public GetBrandRedeemMetricsHandler(
        IBrandAccessService brandAccessService,
        ILoyaltyMetricRepository metricRepository,
        IUserRepository userRepository)
    {
        _brandAccessService = brandAccessService;
        _metricRepository = metricRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<IReadOnlyCollection<MetricResponse>>> Handle(
        GetBrandRedeemMetricsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        if (query.BrandId == Guid.Empty)
            return Result.Fail(BrandErrors.IdIsEmpty());

        var userExists = await _userRepository.ExistsAsync(query.UserId, cancellationToken);
        if (!userExists)
            return Result.Fail(UserErrors.NotFound());

        var canRedeem = await _brandAccessService.CanAsync(
            query.UserId,
            query.BrandId,
            PermissionCode.StampRedeem,
            cancellationToken);

        if (!canRedeem)
            return Result.Fail(AccessErrors.Denied());

        var metrics = await _metricRepository.GetByBrandAsync(query.BrandId, cancellationToken);
        IReadOnlyCollection<MetricResponse> response = metrics
            .Where(metric => metric.IsActive)
            .Select(metric => new MetricResponse(
                metric.Id,
                metric.BrandId,
                metric.Code,
                metric.Name,
                metric.RedemptionAmount,
                metric.IsActive,
                metric.CreatedAt))
            .ToArray();

        return Result.Ok(response);
    }
}
