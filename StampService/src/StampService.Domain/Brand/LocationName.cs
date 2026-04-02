using FluentResults;

namespace StampService.Domain.Brand;

public record LocationName
{
    public string Name { get; init; }

    private LocationName(string name)
    {
        Name = name;
    }

    public static Result<LocationName> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail("name");

        if (name.Length < Constants.MIN_LOCATION_NAME_LENGTH)
            return Result.Fail("name");

        if (name.Length > Constants.MAX_LOCATION_NAME_LENGTH)
            return Result.Fail("name");

        var locationName = new LocationName(name);
        return Result.Ok(locationName);
    }
}