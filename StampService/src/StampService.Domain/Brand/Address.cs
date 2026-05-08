using FluentResults;
using StampService.Domain.Shared;

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
            return Result.Fail(DomainError.Validation(
                "address.city_required",
                "Город не может быть пустым",
                nameof(city)));

        if (city.Length > Constants.MAX_ADDRESS_CITY_LENGTH)
            return Result.Fail(DomainError.Validation(
                "address.city_too_long",
                $"Город не должен превышать {Constants.MAX_ADDRESS_CITY_LENGTH} символов",
                nameof(city)));

        if (string.IsNullOrWhiteSpace(street))
            return Result.Fail(DomainError.Validation(
                "address.street_required",
                "Улица не может быть пустой",
                nameof(street)));

        if (street.Length > Constants.MAX_ADDRESS_STREET_LENGTH)
            return Result.Fail(DomainError.Validation(
                "address.street_too_long",
                $"Улица не должна превышать {Constants.MAX_ADDRESS_STREET_LENGTH} символов",
                nameof(street)));

        if (string.IsNullOrWhiteSpace(houseNumber))
            return Result.Fail(DomainError.Validation(
                "address.house_number_required",
                "Номер дома не может быть пустым",
                nameof(houseNumber)));

        if (houseNumber.Length > Constants.MAX_ADDRESS_HOUSE_NUMBER_LENGTH)
            return Result.Fail(DomainError.Validation(
                "address.house_number_too_long",
                $"Номер дома не должен превышать {Constants.MAX_ADDRESS_HOUSE_NUMBER_LENGTH} символов",
                nameof(houseNumber)));

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
