namespace StampService.TelegramBot.Features.CustomerBalances.Actions;

public record ViewCustomerBalanceHistoryPayload(
    Guid CustomerUserId,
    string CustomerName,
    string CustomerCode,
    Guid MetricDefinitionId,
    string MetricName);
