using FluentResults;
using StampService.Domain.Shared;

namespace StampService.Domain.Loyalty;

public class LoyaltyMetricDefinition : BaseEntity
{
    public Guid BrandId { get; private set; }
    public Brand.Brand Brand { get; private set; } = null!;
    public string Code { get; private set; }
    public string Name { get; private set; }
    public int RedemptionAmount { get; private set; }
    public bool IsActive { get; private set; }

    private LoyaltyMetricDefinition(Guid brandId, string code, string name, int redemptionAmount)
    {
        BrandId = brandId;
        Code = code;
        Name = name;
        RedemptionAmount = redemptionAmount;
        IsActive = true;
    }

    // EF Core
    protected LoyaltyMetricDefinition()
    {
        Code = null!;
        Name = null!;
    }

    public static Result<LoyaltyMetricDefinition> Create(
        Guid brandId,
        string code,
        string name,
        int redemptionAmount)
    {
        if (brandId == Guid.Empty)
            return Result.Fail(DomainError.Validation(
                "metric_definition.brand_id_empty",
                "BrandId не может быть пустым GUID",
                nameof(brandId)));

        if (string.IsNullOrWhiteSpace(code))
            return Result.Fail(DomainError.Validation(
                "metric_definition.code_required",
                "Code не может быть пустым",
                nameof(code)));

        if (code.Length > Constants.MAX_METRIC_CODE_LENGTH)
            return Result.Fail(DomainError.Validation(
                "metric_definition.code_too_long",
                $"Code не должен превышать {Constants.MAX_METRIC_CODE_LENGTH} символов",
                nameof(code)));

        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail(DomainError.Validation(
                "metric_definition.name_required",
                "Name не может быть пустым",
                nameof(name)));

        if (name.Length > Constants.MAX_METRIC_NAME_LENGTH)
            return Result.Fail(DomainError.Validation(
                "metric_definition.name_too_long",
                $"Name не должен превышать {Constants.MAX_METRIC_NAME_LENGTH} символов",
                nameof(name)));

        if (redemptionAmount <= 0)
            return Result.Fail(DomainError.Validation(
                "metric_definition.redemption_amount_must_be_positive",
                "RedemptionAmount must be positive",
                nameof(redemptionAmount)));

        var definition = new LoyaltyMetricDefinition(brandId, code.Trim(), name.Trim(), redemptionAmount);
        return Result.Ok(definition);
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
        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail(DomainError.Validation(
                "metric_definition.name_required",
                "Name не может быть пустым",
                nameof(name)));

        if (name.Length > Constants.MAX_METRIC_NAME_LENGTH)
            return Result.Fail(DomainError.Validation(
                "metric_definition.name_too_long",
                $"Name не должен превышать {Constants.MAX_METRIC_NAME_LENGTH} символов",
                nameof(name)));

        Name = name.Trim();
        Touch();
        return Result.Ok();
    }

    public Result UpdateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Fail(DomainError.Validation(
                "metric_definition.code_required",
                "Code не может быть пустым",
                nameof(code)));

        if (code.Length > Constants.MAX_METRIC_CODE_LENGTH)
            return Result.Fail(DomainError.Validation(
                "metric_definition.code_too_long",
                $"Code не должен превышать {Constants.MAX_METRIC_CODE_LENGTH} символов",
                nameof(code)));

        Code = code.Trim();
        Touch();
        return Result.Ok();
    }
}
