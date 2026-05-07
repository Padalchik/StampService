using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Access;

namespace StampService.Application.Metrics.Queries.GetBrandIssueMetrics;

public class GetBrandIssueMetricsHandler : IQueryHandler<IReadOnlyCollection<MetricResponse>, GetBrandIssueMetricsQuery>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly ILoyaltyMetricRepository _metricRepository;

    public GetBrandIssueMetricsHandler(
        IBrandAccessService brandAccessService,
        ILoyaltyMetricRepository metricRepository)
    {
        _brandAccessService = brandAccessService;
        _metricRepository = metricRepository;
    }

    public async Task<Result<IReadOnlyCollection<MetricResponse>>> Handle(
        GetBrandIssueMetricsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId == Guid.Empty)
            return Result.Fail("User id cannot be empty");

        if (query.BrandId == Guid.Empty)
            return Result.Fail("Brand id cannot be empty");

        var canIssue = await _brandAccessService.CanAsync(
            query.UserId,
            query.BrandId,
            PermissionCode.StampIssue,
            cancellationToken);

        if (!canIssue)
            return Result.Fail("Access denied");

        var metrics = await _metricRepository.GetByBrandAsync(query.BrandId, cancellationToken);
        IReadOnlyCollection<MetricResponse> response = metrics
            .Where(metric => metric.IsActive)
            .Select(metric => new MetricResponse(
                metric.Id,
                metric.BrandId,
                metric.Code,
                metric.Name,
                metric.IsActive,
                metric.CreatedAt))
            .ToArray();

        return Result.Ok(response);
    }
}
