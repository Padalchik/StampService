using System.Security.Claims;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StampService.Application.Abstractions;
using StampService.Application.Metrics.Commands.CreateMetric;
using StampService.Contracts.DTOs.Metrics;

namespace StampService.API.Controllers;

[ApiController]
[Authorize]
[Route("api/brands/{brandId:guid}/metrics")]
public class MetricsController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<MetricResponse>> Create(
        Guid brandId,
        CreateMetricRequest request,
        [FromServices] ICommandHandler<MetricResponse, CreateMetricCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return Unauthorized(userIdResult.Errors);

        var command = new CreateMetricCommand(brandId, userIdResult.Value, request);

        var result = await handler.Handle(command, cancellationToken);

        if (result.IsSuccess)
            return CreatedAtAction(nameof(Create), new { brandId, metricId = result.Value.Id }, result.Value);

        if (result.Errors.Any(error => error.Message == "Access denied"))
            return Forbid();

        return BadRequest(result.Errors);
    }

    private Result<Guid> GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdValue))
            return Result.Fail("User id claim is missing");

        return Guid.TryParse(userIdValue, out var userId)
            ? Result.Ok(userId)
            : Result.Fail("User id claim is invalid");
    }
}
