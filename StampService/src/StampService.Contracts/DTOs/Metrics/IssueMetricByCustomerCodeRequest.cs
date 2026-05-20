namespace StampService.Contracts.DTOs.Metrics;

public record IssueMetricByCustomerCodeRequest(
    string CustomerCode,
    int Amount,
    string? Comment);
