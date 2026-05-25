namespace StampService.TelegramBot.Features.CustomerBalances.Actions;

public record ViewCustomerBalanceHistoryPayload(
    Guid CustomerUserId,
    string CustomerName,
    string CustomerPhoneNumber,
    Guid MetricDefinitionId,
    string MetricName);
