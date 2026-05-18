using FluentResults;
using StampService.Application.Errors;
using StampService.Domain.User;

namespace StampService.Application.Auth;

public static class PhoneNumberNormalizer
{
    public static Result<string> NormalizeForAuth(string? phoneNumber, string? invalidField = null)
    {
        var result = PhoneNumber.Normalize(phoneNumber);
        return result.IsSuccess
            ? Result.Ok(result.Value)
            : Result.Fail(AuthErrors.PhoneInvalid(invalidField));
    }

    public static string Normalize(string? phoneNumber)
    {
        var result = PhoneNumber.Normalize(phoneNumber);
        return result.IsSuccess ? result.Value : string.Empty;
    }
}
