using FluentResults;
using StampService.Domain.Shared;

namespace StampService.Domain.Brand;

public class Brand : BaseEntity
{
    private HashSet<Location> _locations = [];
    
    public string Name { get; private set; }
    
    public IReadOnlySet<Location> Locations => _locations;

    private Brand(string name)
    {
        Name = name;
    }
    
    // EF Core
    protected Brand()
    {
        Name = null!;
    }
    
    public static Result<Brand> Create(string name)
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

        var brand = new Brand(name.Trim());
        return Result.Ok(brand);
    }
}
