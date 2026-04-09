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

    // Навигационные свойства
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
            return Result.Fail("UserId не может быть пустым GUID");

        if (brandId == Guid.Empty)
            return Result.Fail("BrandId не может быть пустым GUID");

        if (roleId == Guid.Empty)
            return Result.Fail("RoleId не может быть пустым GUID");

        var membership = new BrandMembership(userId, brandId, roleId);
        return Result.Ok(membership);
    }
}
