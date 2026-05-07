namespace StampService.TelegramBot.Features.MetricBalances.Actions;

public record ViewBalanceHistoryPayload(
    Guid MetricDefinitionId,
    string BrandName,
    string MetricName);
