using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Loyalty;

namespace StampService.Application.Metrics;

internal static class MetricMapping
{
    public static MetricResponse ToResponse(LoyaltyMetricDefinition metric)
    {
        return new MetricResponse(
            metric.Id,
            metric.BrandId,
            metric.Code,
            metric.Name,
            metric.RedemptionAmount,
            metric.IsActive,
            metric.CreatedAt);
    }
}
