using FluentResults;
using StampService.Application.Errors;
using StampService.Domain.Shared;

namespace StampService.API.EndpointResults;

public static class ErrorMapping
{
    public static int GetStatusCode(IReadOnlyCollection<IError> errors)
    {
        if (errors.Count == 0)
            return StatusCodes.Status500InternalServerError;

        var types = errors
            .Select(GetErrorType)
            .ToArray();

        return types
            .Select(GetStatusCode)
            .OrderByDescending(GetStatusPriority)
            .First();
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
                appError.InvalidField,
                GetCustomMetadata(appError));
        }

        if (error is DomainError domainError)
        {
            return new ApiErrorResponse(
                domainError.Code,
                domainError.Message,
                domainError.Type.ToString(),
                domainError.InvalidField,
                GetCustomMetadata(domainError));
        }

        return new ApiErrorResponse(
            "error.untyped",
            error.Message,
            ResponseErrorType.Failure.ToString(),
            null,
            GetCustomMetadata(error));
    }

    private static ResponseErrorType GetErrorType(IError error)
    {
        return error switch
        {
            AppError appError => MapAppErrorType(appError.Type),
            DomainError domainError => MapDomainErrorType(domainError.Type),
            _ => ResponseErrorType.Failure
        };
    }

    private static int GetStatusCode(ResponseErrorType errorType)
    {
        return errorType switch
        {
            ResponseErrorType.Validation => StatusCodes.Status400BadRequest,
            ResponseErrorType.NotFound => StatusCodes.Status404NotFound,
            ResponseErrorType.Conflict => StatusCodes.Status409Conflict,
            ResponseErrorType.Authentication => StatusCodes.Status401Unauthorized,
            ResponseErrorType.Authorization => StatusCodes.Status403Forbidden,
            ResponseErrorType.Failure => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static int GetStatusPriority(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status500InternalServerError => 100,
            StatusCodes.Status401Unauthorized => 90,
            StatusCodes.Status403Forbidden => 80,
            StatusCodes.Status409Conflict => 70,
            StatusCodes.Status404NotFound => 60,
            StatusCodes.Status400BadRequest => 50,
            _ => 0
        };
    }

    private static IReadOnlyDictionary<string, object>? GetCustomMetadata(IError error)
    {
        var metadata = error.Metadata
            .Where(item => item.Value is not null
                && item.Key is not ("error_code" or "error_type" or "invalid_field"))
            .ToDictionary(item => item.Key, item => item.Value);

        return metadata.Count == 0
            ? null
            : metadata;
    }

    private static ResponseErrorType MapAppErrorType(AppErrorType errorType)
    {
        return errorType switch
        {
            AppErrorType.Validation => ResponseErrorType.Validation,
            AppErrorType.NotFound => ResponseErrorType.NotFound,
            AppErrorType.Conflict => ResponseErrorType.Conflict,
            AppErrorType.Authentication => ResponseErrorType.Authentication,
            AppErrorType.Authorization => ResponseErrorType.Authorization,
            AppErrorType.Failure => ResponseErrorType.Failure,
            _ => ResponseErrorType.Failure
        };
    }

    private static ResponseErrorType MapDomainErrorType(DomainErrorType errorType)
    {
        return errorType switch
        {
            DomainErrorType.Validation => ResponseErrorType.Validation,
            DomainErrorType.NotFound => ResponseErrorType.NotFound,
            DomainErrorType.Conflict => ResponseErrorType.Conflict,
            DomainErrorType.Failure => ResponseErrorType.Failure,
            _ => ResponseErrorType.Failure
        };
    }

    private enum ResponseErrorType
    {
        Validation,
        NotFound,
        Failure,
        Conflict,
        Authentication,
        Authorization
    }
}
