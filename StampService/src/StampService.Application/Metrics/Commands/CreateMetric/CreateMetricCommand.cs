using StampService.Application.Abstractions;
using StampService.Contracts.DTOs.Metrics;

namespace StampService.Application.Metrics.Commands.CreateMetric;

public record CreateMetricCommand(
    Guid BrandId,
    Guid UserId,
    CreateMetricRequest Request) : ICommand;
