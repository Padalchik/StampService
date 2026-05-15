using System.Security.Claims;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using StampService.Application.Errors;

namespace StampService.API.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected Result<Guid> GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdValue))
            return Result.Fail(AuthErrors.UserIdClaimMissing());

        return Guid.TryParse(userIdValue, out var userId)
            ? Result.Ok(userId)
            : Result.Fail(AuthErrors.UserIdClaimInvalid());
    }
}
