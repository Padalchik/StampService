using Microsoft.AspNetCore.Mvc;

namespace StampService.API.EndpointResults;

public sealed class SuccessResult<TValue> : IActionResult
{
    private readonly TValue _value;
    private readonly int _statusCode;

    public SuccessResult(TValue value, int statusCode = StatusCodes.Status200OK)
    {
        _value = value;
        _statusCode = statusCode;
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        context.HttpContext.Response.StatusCode = _statusCode;
        context.HttpContext.Response.ContentType = "application/json";

        await context.HttpContext.Response.WriteAsJsonAsync(Envelope<TValue>.Ok(_value));
    }
}
