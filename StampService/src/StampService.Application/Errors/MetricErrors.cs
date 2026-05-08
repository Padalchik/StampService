namespace StampService.Application.Errors;

public static class MetricErrors
{
    public static AppError NotFound() =>
        AppError.NotFound(
            AppErrorCodes.Metric.NotFound,
            "Metric not found");

    public static AppError BalanceNotFound() =>
        AppError.NotFound(
            AppErrorCodes.MetricBalance.NotFound,
            "Metric balance not found");

    public static AppError InsufficientFunds(int currentBalance, int requiredAmount) =>
        AppError.Conflict(
            AppErrorCodes.MetricBalance.InsufficientFunds,
            $"Insufficient metric balance. Current: {currentBalance}, required: {requiredAmount}")
            .WithMetadataValue("current_balance", currentBalance)
            .WithMetadataValue("required_amount", requiredAmount);

    public static AppError IsNotActive() =>
        AppError.Conflict(
            AppErrorCodes.Metric.Inactive,
            "Metric is not active");

    public static AppError CodeAlreadyExistsForBrand() =>
        AppError.Conflict(
            AppErrorCodes.Metric.CodeAlreadyExistsForBrand,
            "Metric code already exists for this brand");

}
