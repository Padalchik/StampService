using FluentResults;
using StampService.Application.Errors;

namespace StampService.API.EndpointResults;

public static class ErrorMapping
{
    public static int GetStatusCode(IReadOnlyCollection<IError> errors)
    {
        if (errors.Count == 0)
            return StatusCodes.Status500InternalServerError;

        var types = errors
            .Select(GetErrorType)
            .Distinct()
            .ToArray();

        return types.Length == 1
            ? GetStatusCode(types[0])
            : StatusCodes.Status500InternalServerError;
    }

    public static IReadOnlyCollection<ApiErrorResponse> ToResponse(IReadOnlyCollection<IError> errors)
    {
        return errors
            .Select(ToResponse)
            .ToArray();
    }

    public static ApiErrorResponse ToResponse(IError error)
    {
        if (error is AppError appError)
        {
            return new ApiErrorResponse(
                appError.Code,
                appError.Message,
                appError.Type.ToString(),
                appError.InvalidField);
        }

        return new ApiErrorResponse(
            "error.untyped",
            error.Message,
            AppErrorType.Validation.ToString(),
            null);
    }

    private static AppErrorType GetErrorType(IError error)
    {
        return error is AppError appError
            ? appError.Type
            : AppErrorType.Validation;
    }

    private static int GetStatusCode(AppErrorType errorType)
    {
        return errorType switch
        {
            AppErrorType.Validation => StatusCodes.Status400BadRequest,
            AppErrorType.NotFound => StatusCodes.Status404NotFound,
            AppErrorType.Conflict => StatusCodes.Status409Conflict,
            AppErrorType.Authentication => StatusCodes.Status401Unauthorized,
            AppErrorType.Authorization => StatusCodes.Status403Forbidden,
            AppErrorType.Failure => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
