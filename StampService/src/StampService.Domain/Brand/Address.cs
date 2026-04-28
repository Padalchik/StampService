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
        if (string.IsNullOrWhiteSpace(city))
            return Result.Fail("Город не может быть пустым");

        if (city.Length > Constants.MAX_ADDRESS_CITY_LENGTH)
            return Result.Fail($"Город не должен превышать {Constants.MAX_ADDRESS_CITY_LENGTH} символов");

        if (string.IsNullOrWhiteSpace(street))
            return Result.Fail("Улица не может быть пустой");

        if (street.Length > Constants.MAX_ADDRESS_STREET_LENGTH)
            return Result.Fail($"Улица не должна превышать {Constants.MAX_ADDRESS_STREET_LENGTH} символов");

        if (string.IsNullOrWhiteSpace(houseNumber))
            return Result.Fail("Номер дома не может быть пустым");

        if (houseNumber.Length > Constants.MAX_ADDRESS_HOUSE_NUMBER_LENGTH)
            return Result.Fail($"Номер дома не должен превышать {Constants.MAX_ADDRESS_HOUSE_NUMBER_LENGTH} символов");

        var address = new Address(city.Trim(), street.Trim(), houseNumber.Trim());
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