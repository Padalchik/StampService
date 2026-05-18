using StampService.Application.Abstractions;
using StampService.Contracts.DTOs.Profile;

namespace StampService.Application.Users.Queries.GetMyProfile;

public record GetMyProfileQuery(Guid UserId) : IQuery;
