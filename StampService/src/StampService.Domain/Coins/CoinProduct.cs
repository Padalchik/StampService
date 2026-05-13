using FluentResults;
using StampService.Domain.Shared;
using BrandEntity = StampService.Domain.Brand.Brand;

namespace StampService.Domain.Coins;

public class CoinProduct : BaseEntity
{
    public Guid BrandId { get; private set; }
    public BrandEntity Brand { get; private set; } = null!;
    public string Name { get; private set; }
    public int Price { get; private set; }
    public bool IsActive { get; private set; }

    private CoinProduct(Guid brandId, string name, int price)
    {
        BrandId = brandId;
        Name = name;
        Price = price;
        IsActive = true;
    }

    protected CoinProduct()
    {
        Name = null!;
    }

    public static Result<CoinProduct> Create(Guid brandId, string name, int price)
    {
        if (brandId == Guid.Empty)
            return Result.Fail(DomainError.Validation(
                "coin_product.brand_id_empty",
                "BrandId cannot be empty GUID",
                nameof(brandId)));

        var validateNameResult = ValidateName(name);
        if (validateNameResult.IsFailed)
            return Result.Fail(validateNameResult.Errors);

        var validatePriceResult = ValidatePrice(price);
        if (validatePriceResult.IsFailed)
            return Result.Fail(validatePriceResult.Errors);

        return Result.Ok(new CoinProduct(brandId, name.Trim(), price));
    }

    public Result UpdateDetails(string name, int price)
    {
        var validateNameResult = ValidateName(name);
        if (validateNameResult.IsFailed)
            return Result.Fail(validateNameResult.Errors);

        var validatePriceResult = ValidatePrice(price);
        if (validatePriceResult.IsFailed)
            return Result.Fail(validatePriceResult.Errors);

        Name = name.Trim();
        Price = price;
        Touch();

        return Result.Ok();
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        Touch();
    }

    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        Touch();
    }

    private static Result ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail(DomainError.Validation(
                "coin_product.name_required",
                "Name cannot be empty",
                nameof(name)));

        if (name.Length > Constants.MAX_COIN_PRODUCT_NAME_LENGTH)
            return Result.Fail(DomainError.Validation(
                "coin_product.name_too_long",
                $"Name cannot exceed {Constants.MAX_COIN_PRODUCT_NAME_LENGTH} characters",
                nameof(name)));

        return Result.Ok();
    }

    private static Result ValidatePrice(int price)
    {
        if (price <= 0)
            return Result.Fail(DomainError.Validation(
                "coin_product.price_must_be_positive",
                "Price must be positive",
                nameof(price)));

        return Result.Ok();
    }
}
