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
            return Result.Fail("UserId не может быть пустым GUID");

        if (brandId == Guid.Empty)
            return Result.Fail("BrandId не может быть пустым GUID");

        if (metricDefinitionId == Guid.Empty)
            return Result.Fail("MetricDefinitionId не может быть пустым GUID");

        var balance = new MetricBalance(userId, brandId, metricDefinitionId);
        return Result.Ok(balance);
    }

    public Result Add(int amount)
    {
        if (amount <= 0)
            return Result.Fail("Количество для начисления должно быть больше нуля");

        Value += amount;
        Touch();
        return Result.Ok();
    }

    public Result Subtract(int amount)
    {
        if (amount <= 0)
            return Result.Fail("Количество для списания должно быть больше нуля");

        if (Value < amount)
            return Result.Fail($"Недостаточно средств. Текущий баланс: {Value}, требуется: {amount}");

        Value -= amount;
        Touch();
        return Result.Ok();
    }
}
