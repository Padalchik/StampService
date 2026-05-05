namespace StampService.Contracts.DTOs.Metrics;

public record IssueMetricRequest(
    Guid UserId,
    int Amount,
    string Comment);
