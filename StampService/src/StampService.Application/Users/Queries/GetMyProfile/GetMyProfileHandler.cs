using FluentResults;
using System.Text.Json;
using StampService.Application.Abstractions;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.Profile;
using StampService.Domain.User;

namespace StampService.Application.Users.Queries.GetMyProfile;

public class GetMyProfileHandler : IQueryHandler<MyProfileResponse, GetMyProfileQuery>
{
    private readonly IUserRepository _userRepository;

    public GetMyProfileHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<MyProfileResponse>> Handle(
        GetMyProfileQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        var user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null)
            return Result.Fail(UserErrors.NotFound());

        var telegram = user.Identities.FirstOrDefault(identity => identity.Type == IdentityType.Telegram);
        var phone = user.Identities.FirstOrDefault(identity => identity.Type == IdentityType.Phone);

        return Result.Ok(new MyProfileResponse(
            user.Id,
            user.Name,
            new IdentityStatusResponse(telegram is not null, telegram is null ? null : GetTelegramDisplayName(telegram)),
            new IdentityStatusResponse(
                phone is not null,
                phone is null ? null : UserIdentityFormatter.MaskPhone(phone.Key))));
    }

    private static string GetTelegramDisplayName(UserIdentity identity)
    {
        try
        {
            using var document = JsonDocument.Parse(identity.Metadata);
            var root = document.RootElement;

            if (TryGetString(root, "Username", out var username))
                return username;

            if (TryGetString(root, "DisplayName", out var displayName))
                return displayName;

            var firstName = TryGetString(root, "FirstName", out var first) ? first : string.Empty;
            var lastName = TryGetString(root, "LastName", out var last) ? last : string.Empty;
            var fullName = $"{firstName} {lastName}".Trim();
            if (!string.IsNullOrWhiteSpace(fullName))
                return fullName;
        }
        catch (JsonException)
        {
        }

        return identity.Key;
    }

    private static bool TryGetString(JsonElement root, string propertyName, out string value)
    {
        value = string.Empty;

        if (!root.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var propertyValue = property.GetString();
        if (string.IsNullOrWhiteSpace(propertyValue))
            return false;

        value = propertyValue.Trim();
        return true;
    }
}
