namespace StampService.Application.Errors;

public static class CoinErrors
{
    public static AppError WalletNotFound() =>
        AppError.NotFound(
            AppErrorCodes.Coin.WalletNotFound,
            "Coin wallet not found");

    public static AppError InsufficientFunds(int currentBalance, int requiredAmount) =>
        AppError.Conflict(
            AppErrorCodes.Coin.InsufficientFunds,
            $"Insufficient coins. Current: {currentBalance}, required: {requiredAmount}")
            .WithMetadataValue("current_balance", currentBalance)
            .WithMetadataValue("required_amount", requiredAmount);
}
