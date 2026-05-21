using StampService.Application.Abstractions;
using StampService.Contracts.DTOs.Metrics;

namespace StampService.Application.Metrics.Commands.IssueMetric;

public record IssueMetricByPhoneCommand(
    Guid MetricDefinitionId,
    Guid IssuerUserId,
    IssueMetricByPhoneRequest Request) : ICommand;
