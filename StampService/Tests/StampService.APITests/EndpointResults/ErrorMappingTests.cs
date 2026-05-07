using FluentResults;
using Microsoft.AspNetCore.Http;
using StampService.API.EndpointResults;
using StampService.Application.Errors;
using StampService.Domain.Shared;

namespace StampService.APITests.EndpointResults;

public class ErrorMappingTests
{
    [Theory]
    [MemberData(nameof(StatusCodeCases))]
    public void GetStatusCode_ForTypedError_ShouldReturnExpectedStatusCode(
        AppError error,
        int expectedStatusCode)
    {
        var statusCode = ErrorMapping.GetStatusCode([error]);

        Assert.Equal(expectedStatusCode, statusCode);
    }

    [Fact]
    public void GetStatusCode_WhenErrorsHaveDifferentTypes_ShouldReturnInternalServerError()
    {
        var statusCode = ErrorMapping.GetStatusCode([
            UserErrors.NotFound(),
            AccessErrors.Denied()
        ]);

        Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
    }

    [Fact]
    public void ToResponse_ForTypedError_ShouldKeepStructuredErrorData()
    {
        var error = UserErrors.IdIsEmpty();

        var response = ErrorMapping.ToResponse([error]).Single();

        Assert.Equal("user.id_empty", response.Code);
        Assert.Equal("User id cannot be empty", response.Message);
        Assert.Equal(AppErrorType.Validation.ToString(), response.Type);
        Assert.Equal("userId", response.InvalidField);
    }

    [Fact]
    public void ToResponse_ForDomainError_ShouldKeepStructuredErrorData()
    {
        var error = DomainError.Validation(
            "brand.name_required",
            "Name не может быть пустым",
            "name");

        var response = ErrorMapping.ToResponse([error]).Single();

        Assert.Equal("brand.name_required", response.Code);
        Assert.Equal("Name не может быть пустым", response.Message);
        Assert.Equal(DomainErrorType.Validation.ToString(), response.Type);
        Assert.Equal("name", response.InvalidField);
    }

    [Fact]
    public void GetStatusCode_ForDomainConflictError_ShouldReturnConflict()
    {
        var error = DomainError.Conflict(
            "metric_balance.insufficient_funds",
            "Недостаточно средств",
            "amount");

        var statusCode = ErrorMapping.GetStatusCode([error]);

        Assert.Equal(StatusCodes.Status409Conflict, statusCode);
    }

    [Fact]
    public void GetStatusCode_ForAppAndDomainErrorsWithSameType_ShouldReturnThatStatusCode()
    {
        var statusCode = ErrorMapping.GetStatusCode([
            GeneralErrors.ValueIsRequired("name"),
            DomainError.Validation("brand.name_required", "Name не может быть пустым", "name")
        ]);

        Assert.Equal(StatusCodes.Status400BadRequest, statusCode);
    }

    [Fact]
    public void ToResponse_ForUntypedError_ShouldUseFallbackValidationError()
    {
        var response = ErrorMapping.ToResponse([new Error("Legacy domain error")]).Single();

        Assert.Equal("error.untyped", response.Code);
        Assert.Equal("Legacy domain error", response.Message);
        Assert.Equal(AppErrorType.Validation.ToString(), response.Type);
        Assert.Null(response.InvalidField);
    }

    public static TheoryData<AppError, int> StatusCodeCases()
    {
        return new TheoryData<AppError, int>
        {
            { GeneralErrors.ValueIsRequired("name"), StatusCodes.Status400BadRequest },
            { UserErrors.NotFound(), StatusCodes.Status404NotFound },
            { BrandErrors.AlreadyHasOwner(), StatusCodes.Status409Conflict },
            { AuthErrors.UserIdClaimMissing(), StatusCodes.Status401Unauthorized },
            { AccessErrors.Denied(), StatusCodes.Status403Forbidden },
            { GeneralErrors.Failure(), StatusCodes.Status500InternalServerError }
        };
    }
}
