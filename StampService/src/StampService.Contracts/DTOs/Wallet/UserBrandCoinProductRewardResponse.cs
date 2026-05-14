namespace StampService.Contracts.DTOs.Wallet;

public record UserBrandCoinProductRewardResponse(
    Guid ProductId,
    string ProductName,
    int Price,
    int CurrentBalance,
    int MissingAmount,
    bool IsAvailable);
