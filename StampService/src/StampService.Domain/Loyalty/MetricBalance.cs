using FluentResults;
using StampService.Domain.Shared;
using BrandEntity = StampService.Domain.Brand.Brand;
using UserEntity = StampService.Domain.User.User;

namespace StampService.Domain.Loyalty;

public class MetricBalance : BaseEntity
{
    public Guid UserId { get; private set; }
    public UserEntity User { get; private set; } = null!;
    public Guid BrandId { get; private set; }
    public BrandEntity Brand { get; private set; } = null!;
    public Guid MetricDefinitionId { get; private set; }
    public LoyaltyMetricDefinition MetricDefinition { get; private set; } = null!;
    public int Value { get; private set; }
    
    private MetricBalance(Guid userId, Guid brandId, Guid metricDefinitionId)
    {
        UserId = userId;
        BrandId = brandId;
        MetricDefinitionId = metricDefinitionId;
        Value = 0;
    }

    // EF Core
    protected MetricBalance()
    {
    }

    public static Result<MetricBalance> Create(Guid userId, Guid brandId, Guid metricDefinitionId)
    {
        if (userId == Guid.Empty)
            return Result.Fail(DomainError.Validation(
                "metric_balance.user_id_empty",
                "UserId не может быть пустым GUID",
                nameof(userId)));

        if (brandId == Guid.Empty)
            return Result.Fail(DomainError.Validation(
                "metric_balance.brand_id_empty",
                "BrandId не может быть пустым GUID",
                nameof(brandId)));

        if (metricDefinitionId == Guid.Empty)
            return Result.Fail(DomainError.Validation(
                "metric_balance.metric_definition_id_empty",
                "MetricDefinitionId не может быть пустым GUID",
                nameof(metricDefinitionId)));

        var balance = new MetricBalance(userId, brandId, metricDefinitionId);
        return Result.Ok(balance);
    }

    public Result Add(int amount)
    {
        if (amount <= 0)
            return Result.Fail(DomainError.Validation(
                "metric_balance.issue_amount_not_positive",
                "Количество для начисления должно быть больше нуля",
                nameof(amount)));

        Value += amount;
        Touch();
        return Result.Ok();
    }

    public Result Subtract(int amount)
    {
        if (amount <= 0)
            return Result.Fail(DomainError.Validation(
                "metric_balance.redeem_amount_not_positive",
                "Количество для списания должно быть больше нуля",
                nameof(amount)));

        if (Value < amount)
            return Result.Fail(DomainError.Conflict(
                "metric_balance.insufficient_funds",
                $"Недостаточно средств. Текущий баланс: {Value}, требуется: {amount}",
                nameof(amount)));

        Value -= amount;
        Touch();
        return Result.Ok();
    }

    public Result SetMaterializedValue(int value)
    {
        if (value < 0)
            return Result.Fail(DomainError.Validation(
                "metric_balance.materialized_value_negative",
                "Materialized balance value cannot be negative",
                nameof(value)));

        if (Value == value)
            return Result.Ok();

        Value = value;
        Touch();
        return Result.Ok();
    }
}
