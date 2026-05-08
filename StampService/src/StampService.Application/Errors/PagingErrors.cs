namespace StampService.Application.Errors;

public static class PagingErrors
{
    public static AppError SkipCannotBeNegative() =>
        AppError.Validation(
            AppErrorCodes.Paging.SkipNegative,
            "Skip cannot be negative",
            "skip");

    public static AppError TakeOutOfRange(int maxTake) =>
        AppError.Validation(
            AppErrorCodes.Paging.TakeOutOfRange,
            $"Take must be between 1 and {maxTake}",
            "take");
}
