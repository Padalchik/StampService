using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace StampService.API.EndpointResults;

public sealed class ErrorsResult : IActionResult
{
    private readonly IReadOnlyCollection<IError> _errors;

    public ErrorsResult(IReadOnlyCollection<IError> errors)
    {
        _errors = errors;
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var statusCode = ErrorMapping.GetStatusCode(_errors);
        var errors = ErrorMapping.ToResponse(_errors);

        context.HttpContext.Response.StatusCode = statusCode;
        context.HttpContext.Response.ContentType = "application/json";

        await context.HttpContext.Response.WriteAsJsonAsync(Envelope.Error(errors));
    }
}
