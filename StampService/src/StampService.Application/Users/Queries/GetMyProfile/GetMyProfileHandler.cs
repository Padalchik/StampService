using FluentResults;
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
            user.CustomerCode,
            new IdentityStatusResponse(telegram is not null, telegram?.Key),
            new IdentityStatusResponse(
                phone is not null,
                phone is null ? null : UserIdentityFormatter.MaskPhone(phone.Key))));
    }
}
