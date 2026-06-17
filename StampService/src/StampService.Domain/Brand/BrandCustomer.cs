using FluentResults;
using StampService.Domain.Shared;
using UserEntity = StampService.Domain.User.User;

namespace StampService.Domain.Brand;

public class BrandCustomer : BaseEntity
{
    public Guid BrandId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? CreatedByUserId { get; private set; }

    public Brand Brand { get; private set; } = null!;
    public UserEntity User { get; private set; } = null!;
    public UserEntity? CreatedByUser { get; private set; }

    private BrandCustomer(Guid brandId, Guid userId, Guid? createdByUserId)
    {
        BrandId = brandId;
        UserId = userId;
        CreatedByUserId = createdByUserId;
    }

    // EF Core
    protected BrandCustomer()
    {
    }

    public static Result<BrandCustomer> Create(Guid brandId, Guid userId, Guid? createdByUserId)
    {
        if (brandId == Guid.Empty)
            return Result.Fail(DomainError.Validation(
                "brand_customer.brand_id_empty",
                "BrandId cannot be empty GUID",
                nameof(brandId)));

        if (userId == Guid.Empty)
            return Result.Fail(DomainError.Validation(
                "brand_customer.user_id_empty",
                "UserId cannot be empty GUID",
                nameof(userId)));

        if (createdByUserId == Guid.Empty)
            return Result.Fail(DomainError.Validation(
                "brand_customer.created_by_user_id_empty",
                "CreatedByUserId cannot be empty GUID",
                nameof(createdByUserId)));

        return Result.Ok(new BrandCustomer(brandId, userId, createdByUserId));
    }
}
