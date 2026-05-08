using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Access;

namespace StampService.Application.Metrics.Queries.GetBrandManageMetrics;

public class GetBrandManageMetricsHandler : IQueryHandler<IReadOnlyCollection<MetricResponse>, GetBrandManageMetricsQuery>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly ILoyaltyMetricRepository _metricRepository;
    private readonly IUserRepository _userRepository;

    public GetBrandManageMetricsHandler(
        IBrandAccessService brandAccessService,
        ILoyaltyMetricRepository metricRepository,
        IUserRepository userRepository)
    {
        _brandAccessService = brandAccessService;
        _metricRepository = metricRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<IReadOnlyCollection<MetricResponse>>> Handle(
        GetBrandManageMetricsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        if (query.BrandId == Guid.Empty)
            return Result.Fail(BrandErrors.IdIsEmpty());

        var userExists = await _userRepository.ExistsAsync(query.UserId, cancellationToken);
        if (!userExists)
            return Result.Fail(UserErrors.NotFound());

        var canManage = await _brandAccessService.CanAsync(
            query.UserId,
            query.BrandId,
            PermissionCode.MetricManage,
            cancellationToken);

        if (!canManage)
            return Result.Fail(AccessErrors.Denied());

        var metrics = await _metricRepository.GetByBrandAsync(query.BrandId, cancellationToken);
        IReadOnlyCollection<MetricResponse> response = metrics
            .Select(MetricMapping.ToResponse)
            .ToArray();

        return Result.Ok(response);
    }
}
