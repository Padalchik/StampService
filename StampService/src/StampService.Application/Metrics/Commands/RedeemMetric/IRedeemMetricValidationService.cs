using FluentResults;

namespace StampService.Application.Metrics.Commands.RedeemMetric;

public interface IRedeemMetricValidationService
{
    Task<Result<RedeemMetricPrecheckResult>> ValidateAsync(
        Guid metricDefinitionId,
        Guid redeemerUserId,
        string redemptionCode,
        CancellationToken cancellationToken);
}
