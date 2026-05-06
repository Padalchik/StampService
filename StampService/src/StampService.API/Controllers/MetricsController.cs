using System.Security.Claims;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StampService.Application.Abstractions;
using StampService.Application.Metrics.Commands.CreateMetric;
using StampService.Application.Metrics.Commands.IssueMetric;
using StampService.Application.Metrics.Queries.GetMetricBalance;
using StampService.Application.Metrics.Queries.GetMetricTransactions;
using StampService.Contracts.DTOs.Metrics;

namespace StampService.API.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class MetricsController : ControllerBase
{
    [HttpPost("brands/{brandId:guid}/metrics")]
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

    [HttpPost("metrics/{metricDefinitionId:guid}/issue")]
    public async Task<ActionResult<IssueMetricResponse>> Issue(
        Guid metricDefinitionId,
        IssueMetricRequest request,
        [FromServices] ICommandHandler<IssueMetricResponse, IssueMetricCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return Unauthorized(userIdResult.Errors);

        var command = new IssueMetricCommand(
            metricDefinitionId,
            userIdResult.Value,
            request);

        var result = await handler.Handle(command, cancellationToken);

        if (result.IsSuccess)
            return Ok(result.Value);

        if (result.Errors.Any(error => error.Message == "Access denied"))
            return Forbid();

        return BadRequest(result.Errors);
    }

    [HttpGet("metrics/{metricDefinitionId:guid}/balances/{userId:guid}")]
    public async Task<ActionResult<MetricBalanceResponse>> GetBalance(
        Guid metricDefinitionId,
        Guid userId,
        [FromServices] IQueryHandler<MetricBalanceResponse, GetMetricBalanceQuery> handler,
        CancellationToken cancellationToken)
    {
        var requestUserIdResult = GetUserId();
        if (requestUserIdResult.IsFailed)
            return Unauthorized(requestUserIdResult.Errors);

        var query = new GetMetricBalanceQuery(
            metricDefinitionId,
            userId,
            requestUserIdResult.Value);

        var result = await handler.Handle(query, cancellationToken);

        if (result.IsSuccess)
            return Ok(result.Value);

        if (result.Errors.Any(error => error.Message == "Access denied"))
            return Forbid();

        return BadRequest(result.Errors);
    }

    [HttpGet("metrics/{metricDefinitionId:guid}/transactions")]
    public async Task<ActionResult<MetricTransactionsResponse>> GetTransactions(
        Guid metricDefinitionId,
        [FromQuery] Guid userId,
        [FromQuery] int? skip,
        [FromQuery] int? take,
        [FromServices] IQueryHandler<MetricTransactionsResponse, GetMetricTransactionsQuery> handler,
        CancellationToken cancellationToken)
    {
        var requestUserIdResult = GetUserId();
        if (requestUserIdResult.IsFailed)
            return Unauthorized(requestUserIdResult.Errors);

        var query = new GetMetricTransactionsQuery(
            metricDefinitionId,
            userId,
            requestUserIdResult.Value,
            skip ?? 0,
            take ?? 50);

        var result = await handler.Handle(query, cancellationToken);

        if (result.IsSuccess)
            return Ok(result.Value);

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
