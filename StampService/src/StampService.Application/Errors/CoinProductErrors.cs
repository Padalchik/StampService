namespace StampService.Application.Errors;

public static class CoinProductErrors
{
    public static AppError NotFound() =>
        AppError.NotFound(
            AppErrorCodes.CoinProduct.NotFound,
            "Coin product not found");

    public static AppError IsNotActive() =>
        AppError.Conflict(
            AppErrorCodes.CoinProduct.Inactive,
            "Coin product is not active");
}
