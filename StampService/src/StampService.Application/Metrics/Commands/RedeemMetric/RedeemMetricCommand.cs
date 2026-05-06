using StampService.Application.Abstractions;
using StampService.Contracts.DTOs.Metrics;

namespace StampService.Application.Metrics.Commands.RedeemMetric;

public record RedeemMetricCommand(
    Guid MetricDefinitionId,
    Guid RedeemerUserId,
    RedeemMetricRequest Request) : ICommand;
