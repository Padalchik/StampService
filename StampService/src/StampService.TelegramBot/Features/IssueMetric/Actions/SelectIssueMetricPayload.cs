namespace StampService.TelegramBot.Features.IssueMetric.Actions;

public record SelectIssueMetricPayload(
    Guid MetricDefinitionId,
    string MetricName);
