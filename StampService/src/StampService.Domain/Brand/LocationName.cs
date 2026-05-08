using FluentResults;
using StampService.Domain.Shared;

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
            return Result.Fail(DomainError.Validation(
                "location_name.name_required",
                "Название точки не может быть пустым",
                nameof(name)));

        if (name.Length < Constants.MIN_LOCATION_NAME_LENGTH)
            return Result.Fail(DomainError.Validation(
                "location_name.name_too_short",
                $"Название точки должно быть не менее {Constants.MIN_LOCATION_NAME_LENGTH} символов",
                nameof(name)));

        if (name.Length > Constants.MAX_LOCATION_NAME_LENGTH)
            return Result.Fail(DomainError.Validation(
                "location_name.name_too_long",
                $"Название точки не должно превышать {Constants.MAX_LOCATION_NAME_LENGTH} символов",
                nameof(name)));

        var locationName = new LocationName(name.Trim());
        return Result.Ok(locationName);
    }
}
