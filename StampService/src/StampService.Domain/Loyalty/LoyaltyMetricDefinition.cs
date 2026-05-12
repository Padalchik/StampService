using FluentResults;
using StampService.Domain.Shared;

namespace StampService.Domain.Loyalty;

public class LoyaltyMetricDefinition : BaseEntity
{
    public Guid BrandId { get; private set; }
    public Brand.Brand Brand { get; private set; } = null!;
    public string Name { get; private set; }
    public int RedemptionAmount { get; private set; }
    public bool IsActive { get; private set; }

    private LoyaltyMetricDefinition(Guid brandId, string name, int redemptionAmount)
    {
        BrandId = brandId;
        Name = name;
        RedemptionAmount = redemptionAmount;
        IsActive = true;
    }

    // EF Core
    protected LoyaltyMetricDefinition()
    {
        Name = null!;
    }

    public static Result<LoyaltyMetricDefinition> Create(
        Guid brandId,
        string name,
        int redemptionAmount)
    {
        if (brandId == Guid.Empty)
            return Result.Fail(DomainError.Validation(
                "metric_definition.brand_id_empty",
                "BrandId cannot be empty GUID",
                nameof(brandId)));

        var validateNameResult = ValidateName(name);
        if (validateNameResult.IsFailed)
            return Result.Fail(validateNameResult.Errors);

        var validateRedemptionAmountResult = ValidateRedemptionAmount(redemptionAmount);
        if (validateRedemptionAmountResult.IsFailed)
            return Result.Fail(validateRedemptionAmountResult.Errors);

        return Result.Ok(new LoyaltyMetricDefinition(brandId, name.Trim(), redemptionAmount));
    }

    public void Deactivate()
    {
        if (IsActive == false)
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

    public void Restore(bool activate = false)
    {
        base.Restore();

        if (activate)
            Activate();
    }

    public Result UpdateName(string name)
    {
        var validateNameResult = ValidateName(name);
        if (validateNameResult.IsFailed)
            return Result.Fail(validateNameResult.Errors);

        Name = name.Trim();
        Touch();
        return Result.Ok();
    }

    public Result UpdateRedemptionAmount(int redemptionAmount)
    {
        var validateRedemptionAmountResult = ValidateRedemptionAmount(redemptionAmount);
        if (validateRedemptionAmountResult.IsFailed)
            return Result.Fail(validateRedemptionAmountResult.Errors);

        RedemptionAmount = redemptionAmount;
        Touch();
        return Result.Ok();
    }

    public Result UpdateDetails(string name, int redemptionAmount)
    {
        var validateNameResult = ValidateName(name);
        if (validateNameResult.IsFailed)
            return Result.Fail(validateNameResult.Errors);

        var validateRedemptionAmountResult = ValidateRedemptionAmount(redemptionAmount);
        if (validateRedemptionAmountResult.IsFailed)
            return Result.Fail(validateRedemptionAmountResult.Errors);

        Name = name.Trim();
        RedemptionAmount = redemptionAmount;
        Touch();

        return Result.Ok();
    }

    private static Result ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail(DomainError.Validation(
                "metric_definition.name_required",
                "Name cannot be empty",
                nameof(name)));

        if (name.Length > Constants.MAX_METRIC_NAME_LENGTH)
            return Result.Fail(DomainError.Validation(
                "metric_definition.name_too_long",
                $"Name cannot exceed {Constants.MAX_METRIC_NAME_LENGTH} characters",
                nameof(name)));

        return Result.Ok();
    }

    private static Result ValidateRedemptionAmount(int redemptionAmount)
    {
        if (redemptionAmount <= 0)
            return Result.Fail(DomainError.Validation(
                "metric_definition.redemption_amount_must_be_positive",
                "RedemptionAmount must be positive",
                nameof(redemptionAmount)));

        return Result.Ok();
    }
}
