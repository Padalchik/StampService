using FluentResults;
using StampService.Domain.Shared;
using BrandEntity = StampService.Domain.Brand.Brand;
using UserEntity = StampService.Domain.User.User;

namespace StampService.Domain.Coins;

public class CoinWallet : BaseEntity
{
    public Guid UserId { get; private set; }
    public UserEntity User { get; private set; } = null!;
    public Guid BrandId { get; private set; }
    public BrandEntity Brand { get; private set; } = null!;
    public int Value { get; private set; }

    private CoinWallet(Guid userId, Guid brandId)
    {
        UserId = userId;
        BrandId = brandId;
        Value = 0;
    }

    // EF Core
    protected CoinWallet()
    {
    }

    public static Result<CoinWallet> Create(Guid userId, Guid brandId)
    {
        if (userId == Guid.Empty)
            return Result.Fail(DomainError.Validation(
                "coin_wallet.user_id_empty",
                "UserId cannot be empty GUID",
                nameof(userId)));

        if (brandId == Guid.Empty)
            return Result.Fail(DomainError.Validation(
                "coin_wallet.brand_id_empty",
                "BrandId cannot be empty GUID",
                nameof(brandId)));

        return Result.Ok(new CoinWallet(userId, brandId));
    }

    public Result Add(int amount)
    {
        if (amount <= 0)
            return Result.Fail(DomainError.Validation(
                "coin_wallet.issue_amount_not_positive",
                "Coin issue amount must be greater than zero",
                nameof(amount)));

        Value += amount;
        Touch();
        return Result.Ok();
    }

    public Result Subtract(int amount)
    {
        if (amount <= 0)
            return Result.Fail(DomainError.Validation(
                "coin_wallet.redeem_amount_not_positive",
                "Coin redeem amount must be greater than zero",
                nameof(amount)));

        if (Value < amount)
            return Result.Fail(DomainError.Conflict(
                "coin_wallet.insufficient_funds",
                $"Insufficient coins. Current balance: {Value}, required: {amount}",
                nameof(amount)));

        Value -= amount;
        Touch();
        return Result.Ok();
    }

    public Result SetMaterializedValue(int value)
    {
        if (value < 0)
            return Result.Fail(DomainError.Validation(
                "coin_wallet.materialized_value_negative",
                "Coin wallet value cannot be negative",
                nameof(value)));

        if (Value == value)
            return Result.Ok();

        Value = value;
        Touch();
        return Result.Ok();
    }
}
