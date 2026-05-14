using FluentResults;
using StampService.Domain.Shared;

namespace StampService.Domain.Brand;

public class Brand : BaseEntity
{
    private HashSet<Location> _locations = [];
    
    public string Name { get; private set; }
    public bool IsMetricsEnabled { get; private set; }
    public bool IsCoinsEnabled { get; private set; }
    
    public IReadOnlySet<Location> Locations => _locations;

    private Brand(string name)
    {
        Name = name;
        IsMetricsEnabled = true;
        IsCoinsEnabled = true;
    }
    
    // EF Core
    protected Brand()
    {
        Name = null!;
    }
    
    public static Result<Brand> Create(string name)
    {
        var validationResult = ValidateDetails(name, isMetricsEnabled: true, isCoinsEnabled: true);
        if (validationResult.IsFailed)
            return Result.Fail(validationResult.Errors);

        var brand = new Brand(name.Trim());
        return Result.Ok(brand);
    }

    public Result UpdateDetails(string name, bool isMetricsEnabled, bool isCoinsEnabled)
    {
        var validationResult = ValidateDetails(name, isMetricsEnabled, isCoinsEnabled);
        if (validationResult.IsFailed)
            return Result.Fail(validationResult.Errors);

        Name = name.Trim();
        IsMetricsEnabled = isMetricsEnabled;
        IsCoinsEnabled = isCoinsEnabled;
        Touch();

        return Result.Ok();
    }

    private static Result ValidateDetails(string name, bool isMetricsEnabled, bool isCoinsEnabled)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail(DomainError.Validation(
                "brand.name_required",
                "Name не может быть пустым",
                nameof(name)));

        if (name.Length < Constants.MIN_BRAND_NAME_LENGTH || name.Length > Constants.MAX_BRAND_NAME_LENGTH)
            return Result.Fail(DomainError.Validation(
                "brand.name_length_invalid",
                $"Name должен быть от {Constants.MIN_BRAND_NAME_LENGTH} до {Constants.MAX_BRAND_NAME_LENGTH} символов",
                nameof(name)));

        if (!isMetricsEnabled && !isCoinsEnabled)
            return Result.Fail(DomainError.Validation(
                "brand.reward_types_required",
                "At least one reward type must be enabled",
                nameof(isMetricsEnabled)));

        return Result.Ok();
    }
}
