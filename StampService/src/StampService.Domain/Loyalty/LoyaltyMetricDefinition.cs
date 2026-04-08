using FluentResults;
using StampService.Domain.Shared;

namespace StampService.Domain.Loyalty;

public class LoyaltyMetricDefinition : BaseEntity
{
    public Guid BrandId { get; private set; }
    public Brand.Brand Brand { get; private set; } = null!;
    public string Code { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }

    private LoyaltyMetricDefinition(Guid brandId, string code, string name)
    {
        BrandId = brandId;
        Code = code;
        Name = name;
        IsActive = true;
    }

    // EF Core
    protected LoyaltyMetricDefinition()
    {
    }

    public static Result<LoyaltyMetricDefinition> Create(Guid brandId, string code, string name)
    {
        if (brandId == Guid.Empty)
            return Result.Fail("BrandId не может быть пустым GUID");

        if (string.IsNullOrWhiteSpace(code))
            return Result.Fail("Code не может быть пустым");

        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail("Name не может быть пустым");

        var definition = new LoyaltyMetricDefinition(brandId, code.Trim(), name.Trim());
        return Result.Ok(definition);
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public Result UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail("Name не может быть пустым");

        Name = name.Trim();
        return Result.Ok();
    }

    public Result UpdateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Fail("Code не может быть пустым");

        Code = code.Trim();
        return Result.Ok();
    }
}