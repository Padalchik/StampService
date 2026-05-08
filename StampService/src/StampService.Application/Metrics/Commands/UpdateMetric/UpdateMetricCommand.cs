using StampService.Application.Abstractions;
using StampService.Contracts.DTOs.Metrics;

namespace StampService.Application.Metrics.Commands.UpdateMetric;

public record UpdateMetricCommand(
    Guid MetricDefinitionId,
    Guid UserId,
    UpdateMetricRequest Request) : ICommand;
