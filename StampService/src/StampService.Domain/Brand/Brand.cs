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
        
    }
    
    public static Result<Brand> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail("Name не может быть пустым");

        var brand = new Brand(name);
        return Result.Ok(brand);
    }
}