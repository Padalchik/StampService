using FluentResults;
using StampService.Domain.Brand;
using StampService.Domain.Shared;
using BrandEntity = StampService.Domain.Brand.Brand;
using UserEntity = StampService.Domain.User.User;

namespace StampService.Domain.Access;

public class BrandMembership : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid BrandId { get; private set; }
    public Guid RoleId { get; private set; }

    public UserEntity User { get; private set; } = null!;
    public BrandEntity Brand { get; private set; } = null!;
    public Role Role { get; private set; } = null!;

    private BrandMembership(Guid userId, Guid brandId, Guid roleId)
    {
        UserId = userId;
        BrandId = brandId;
        RoleId = roleId;
    }

    // EF Core
    protected BrandMembership()
    {
    }

    public static Result<BrandMembership> Create(Guid userId, Guid brandId, Guid roleId)
    {
        if (userId == Guid.Empty)
            return Result.Fail(DomainError.Validation(
                "brand_membership.user_id_empty",
                "UserId cannot be empty GUID",
                nameof(userId)));

        if (brandId == Guid.Empty)
            return Result.Fail(DomainError.Validation(
                "brand_membership.brand_id_empty",
                "BrandId cannot be empty GUID",
                nameof(brandId)));

        if (roleId == Guid.Empty)
            return Result.Fail(DomainError.Validation(
                "brand_membership.role_id_empty",
                "RoleId cannot be empty GUID",
                nameof(roleId)));

        var membership = new BrandMembership(userId, brandId, roleId);
        return Result.Ok(membership);
    }

    public Result ChangeRole(Guid roleId)
    {
        if (roleId == Guid.Empty)
            return Result.Fail(DomainError.Validation(
                "brand_membership.role_id_empty",
                "RoleId cannot be empty GUID",
                nameof(roleId)));

        if (RoleId == roleId)
            return Result.Ok();

        RoleId = roleId;
        Touch();
        return Result.Ok();
    }

    public Result ReassignToUser(Guid userId)
    {
        if (userId == Guid.Empty)
            return Result.Fail(DomainError.Validation(
                "brand_membership.user_id_empty",
                "UserId cannot be empty GUID",
                nameof(userId)));

        if (UserId == userId)
            return Result.Ok();

        UserId = userId;
        Touch();
        return Result.Ok();
    }
}
