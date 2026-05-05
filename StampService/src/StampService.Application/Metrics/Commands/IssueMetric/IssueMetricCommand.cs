using StampService.Application.Abstractions;
using StampService.Contracts.DTOs.Metrics;

namespace StampService.Application.Metrics.Commands.IssueMetric;

public record IssueMetricCommand(
    Guid MetricDefinitionId,
    Guid IssuerUserId,
    IssueMetricRequest Request) : ICommand;
