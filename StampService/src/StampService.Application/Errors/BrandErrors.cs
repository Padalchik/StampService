namespace StampService.Application.Errors;

public static class BrandErrors
{
    public static AppError NotFound() =>
        AppError.NotFound(
            AppErrorCodes.Brand.NotFound,
            "Brand not found");

    public static AppError IdIsEmpty() =>
        AppError.Validation(
            AppErrorCodes.Brand.IdEmpty,
            "Brand id cannot be empty",
            "brandId");

    public static AppError MetricsDisabled() =>
        AppError.Conflict(
            AppErrorCodes.Brand.MetricsDisabled,
            "Metrics are disabled for this brand");

    public static AppError CoinsDisabled() =>
        AppError.Conflict(
            AppErrorCodes.Brand.CoinsDisabled,
            "Coins are disabled for this brand");

    public static AppError OwnerRoleNotFound() =>
        AppError.Failure(
            AppErrorCodes.Role.OwnerNotFound,
            "Owner role not found");

    public static AppError StaffRoleNotFound() =>
        AppError.Failure(
            AppErrorCodes.Role.StaffNotFound,
            "Staff role not found");

    public static AppError AlreadyHasOwner() =>
        AppError.Conflict(
            AppErrorCodes.Brand.OwnerAlreadyExists,
            "Brand already has an owner");

    public static AppError MembershipNotFound() =>
        AppError.NotFound(
            AppErrorCodes.BrandMembership.NotFound,
            "Brand membership not found");

    public static AppError CannotChangeOwnerRole() =>
        AppError.Conflict(
            AppErrorCodes.BrandMembership.CannotChangeOwnerRole,
            "Cannot change owner role");
}
