namespace StampService.Contracts.DTOs.Wallet;

public record UserBrandMetricRewardResponse(
    Guid MetricDefinitionId,
    string MetricName,
    int CurrentBalance,
    int RequiredAmount,
    int MissingAmount,
    bool IsAvailable);
