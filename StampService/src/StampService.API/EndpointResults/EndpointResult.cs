using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace StampService.API.EndpointResults;

public sealed class EndpointResult<TValue> : IActionResult
{
    private readonly IActionResult _result;

    public EndpointResult(Result<TValue> result, int successStatusCode = StatusCodes.Status200OK)
    {
        _result = result.IsSuccess
            ? new SuccessResult<TValue>(result.Value, successStatusCode)
            : new ErrorsResult(result.Errors);
    }

    public Task ExecuteResultAsync(ActionContext context)
    {
        return _result.ExecuteResultAsync(context);
    }

    public static EndpointResult<TValue> Ok(Result<TValue> result) => new(result);

    public static EndpointResult<TValue> Created(Result<TValue> result) =>
        new(result, StatusCodes.Status201Created);

    public static implicit operator EndpointResult<TValue>(Result<TValue> result) => new(result);
}
