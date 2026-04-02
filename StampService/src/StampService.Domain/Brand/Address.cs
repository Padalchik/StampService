using FluentResults;

namespace StampService.Domain.Brand;

public record Address
{
    public string City { get; }
    public string Street { get; }
    public string HouseNumber { get; }

    private Address(string city, string street, string houseNumber)
    {
        City = city;
        Street = street;
        HouseNumber = houseNumber;
    }

    public static Result<Address> Create(string city, string street, string houseNumber)
    {
        if (string.IsNullOrEmpty(city))
            return Result.Fail("city");

        if (string.IsNullOrEmpty(street))
            return Result.Fail("street");

        if (string.IsNullOrEmpty(houseNumber))
            return Result.Fail("houseNumber");

        var address = new Address(city, street, houseNumber);
        return Result.Ok(address);
    }

    public override string ToString()
    {
        var partOfAddress = new List<string>()
        {
            City,
            Street,
            HouseNumber,
        };

        var filteredParts = partOfAddress.Where(part => string.IsNullOrWhiteSpace(part) == false);
        return string.Join(", ", filteredParts);
    }
}