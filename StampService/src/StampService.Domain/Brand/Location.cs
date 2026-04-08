using StampService.Domain.Shared;

namespace StampService.Domain.Brand;

public class Location : BaseEntity
{
    public LocationName Name { get; private set; } = null!;
    
    public Brand Brand { get; private set; } = null!;

    public Guid BrandId { get; private set; }
    
    public Address Address { get; private set; }
    
    public bool IsActive { get; private set; }
    
    public Location(Brand brand, LocationName name, Address address)
    {
        Name = name;
        Brand = brand;
        BrandId = brand.Id;
        Address = address;
        IsActive = true;
    }

    // EF C0RE
    private Location()
    {
    }
}