namespace StampService.Application.Errors;

public static class MetricErrors
{
    public static AppError NotFound() =>
        AppError.NotFound(
            "metric.not_found",
            "Metric not found");

    public static AppError BalanceNotFound() =>
        AppError.NotFound(
            "metric_balance.not_found",
            "Metric balance not found");

    public static AppError IsNotActive() =>
        AppError.Conflict(
            "metric.inactive",
            "Metric is not active");

    public static AppError CodeAlreadyExistsForBrand() =>
        AppError.Conflict(
            "metric.code_already_exists_for_brand",
            "Metric code already exists for this brand");

    public static AppError SkipCannotBeNegative() =>
        AppError.Validation(
            "paging.skip_negative",
            "Skip cannot be negative",
            "skip");

    public static AppError TakeOutOfRange(int maxTake) =>
        AppError.Validation(
            "paging.take_out_of_range",
            $"Take must be between 1 and {maxTake}",
            "take");
}
