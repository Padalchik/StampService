using StampService.Domain.Loyalty;

namespace StampService.Application.Metrics.Commands.RedeemMetric;

public record RedeemMetricPrecheckResult(
    LoyaltyMetricDefinition Metric,
    Guid CustomerUserId,
    int CurrentBalanceValue);
