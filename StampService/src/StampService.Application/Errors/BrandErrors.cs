namespace StampService.Application.Errors;

public static class BrandErrors
{
    public static AppError NotFound() =>
        AppError.NotFound(
            "brand.not_found",
            "Brand not found");

    public static AppError IdIsEmpty() =>
        AppError.Validation(
            "brand.id_empty",
            "Brand id cannot be empty",
            "brandId");

    public static AppError OwnerRoleNotFound() =>
        AppError.Failure(
            "role.owner_not_found",
            "Owner role not found");

    public static AppError StaffRoleNotFound() =>
        AppError.Failure(
            "role.staff_not_found",
            "Staff role not found");

    public static AppError AlreadyHasOwner() =>
        AppError.Conflict(
            "brand.owner_already_exists",
            "Brand already has an owner");

    public static AppError MembershipNotFound() =>
        AppError.NotFound(
            "brand_membership.not_found",
            "Brand membership not found");

    public static AppError CannotChangeOwnerRole() =>
        AppError.Conflict(
            "brand_membership.cannot_change_owner_role",
            "Cannot change owner role");
}
