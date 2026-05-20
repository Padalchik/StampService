namespace StampService.Contracts.DTOs.Metrics;

public record IssueMetricByPhoneRequest(
    string PhoneNumber,
    int Amount,
    string? Comment);
