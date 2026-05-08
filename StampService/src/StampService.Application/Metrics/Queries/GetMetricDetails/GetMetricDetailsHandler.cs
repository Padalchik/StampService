using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Access;

namespace StampService.Application.Metrics.Queries.GetMetricDetails;

public class GetMetricDetailsHandler : IQueryHandler<MetricResponse, GetMetricDetailsQuery>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly ILoyaltyMetricRepository _metricRepository;

    public GetMetricDetailsHandler(
        IBrandAccessService brandAccessService,
        ILoyaltyMetricRepository metricRepository)
    {
        _brandAccessService = brandAccessService;
        _metricRepository = metricRepository;
    }

    public async Task<Result<MetricResponse>> Handle(
        GetMetricDetailsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        var metric = await _metricRepository.GetByIdAsync(query.MetricDefinitionId, cancellationToken);
        if (metric is null)
            return Result.Fail(MetricErrors.NotFound());

        var canManage = await _brandAccessService.CanAsync(
            query.UserId,
            metric.BrandId,
            PermissionCode.MetricManage,
            cancellationToken);

        if (!canManage)
            return Result.Fail(AccessErrors.Denied());

        return Result.Ok(MetricMapping.ToResponse(metric));
    }
}
