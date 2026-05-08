using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Access;

namespace StampService.Application.Metrics.Commands.UpdateMetric;

public class UpdateMetricHandler : ICommandHandler<MetricResponse, UpdateMetricCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly ILoyaltyMetricRepository _metricRepository;

    public UpdateMetricHandler(
        IBrandAccessService brandAccessService,
        ILoyaltyMetricRepository metricRepository)
    {
        _brandAccessService = brandAccessService;
        _metricRepository = metricRepository;
    }

    public async Task<Result<MetricResponse>> Handle(
        UpdateMetricCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        var metric = await _metricRepository.GetByIdForUpdateAsync(
            command.MetricDefinitionId,
            cancellationToken);

        if (metric is null)
            return Result.Fail(MetricErrors.NotFound());

        var canManage = await _brandAccessService.CanAsync(
            command.UserId,
            metric.BrandId,
            PermissionCode.MetricManage,
            cancellationToken);

        if (!canManage)
            return Result.Fail(AccessErrors.Denied());

        var newCode = command.Request.Code.Trim();
        if (!string.Equals(metric.Code, newCode, StringComparison.Ordinal))
        {
            var codeExists = await _metricRepository.CodeExistsAsync(
                metric.BrandId,
                newCode,
                cancellationToken);

            if (codeExists)
                return Result.Fail(MetricErrors.CodeAlreadyExistsForBrand());
        }

        var updateDetailsResult = metric.UpdateDetails(
            command.Request.Code,
            command.Request.Name,
            command.Request.RedemptionAmount);

        if (updateDetailsResult.IsFailed)
            return Result.Fail(updateDetailsResult.Errors);

        await _metricRepository.SaveAsync(cancellationToken);

        return Result.Ok(MetricMapping.ToResponse(metric));
    }
}
