using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Brands;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Access;
using StampService.Domain.Loyalty;

namespace StampService.Application.Metrics.Commands.CreateMetric;

public class CreateMetricHandler : ICommandHandler<MetricResponse, CreateMetricCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandRepository _brandRepository;
    private readonly ILoyaltyMetricRepository _metricRepository;

    public CreateMetricHandler(
        IBrandAccessService brandAccessService,
        IBrandRepository brandRepository,
        ILoyaltyMetricRepository metricRepository)
    {
        _brandAccessService = brandAccessService;
        _brandRepository = brandRepository;
        _metricRepository = metricRepository;
    }

    public async Task<Result<MetricResponse>> Handle(
        CreateMetricCommand command,
        CancellationToken cancellationToken)
    {
        var brandExists = await _brandRepository.ExistsAsync(command.BrandId, cancellationToken);
        if (!brandExists)
            return Result.Fail("Brand not found");

        var canManageMetrics = await _brandAccessService.CanAsync(
            command.UserId,
            command.BrandId,
            PermissionCode.MetricManage,
            cancellationToken);

        if (!canManageMetrics)
            return Result.Fail("Access denied");

        var metricResult = LoyaltyMetricDefinition.Create(
            command.BrandId,
            command.Request.Code,
            command.Request.Name);

        if (metricResult.IsFailed)
            return Result.Fail(metricResult.Errors);

        var metric = metricResult.Value;

        var codeExists = await _metricRepository.CodeExistsAsync(
            metric.BrandId,
            metric.Code,
            cancellationToken);

        if (codeExists)
            return Result.Fail("Metric code already exists for this brand");

        _metricRepository.Add(metric);
        await _metricRepository.SaveAsync(cancellationToken);

        var response = new MetricResponse(
            metric.Id,
            metric.BrandId,
            metric.Code,
            metric.Name,
            metric.IsActive,
            metric.CreatedAt);

        return Result.Ok(response);
    }
}
