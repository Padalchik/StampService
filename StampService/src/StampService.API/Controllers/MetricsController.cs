using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StampService.API.EndpointResults;
using StampService.Application.Abstractions;
using StampService.Application.Metrics.Commands.CreateMetric;
using StampService.Application.Metrics.Commands.IssueMetric;
using StampService.Application.Metrics.Commands.RedeemMetric;
using StampService.Application.Metrics.Commands.UpdateMetric;
using StampService.Application.Metrics.Queries.GetBrandIssueMetrics;
using StampService.Application.Metrics.Queries.GetBrandManageMetrics;
using StampService.Application.Metrics.Queries.GetMetricBalance;
using StampService.Application.Metrics.Queries.GetMetricDetails;
using StampService.Application.Metrics.Queries.GetMetricTransactions;
using StampService.Application.Metrics.Queries.GetRedeemMetricOptions;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Metrics;

namespace StampService.API.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class MetricsController : ApiControllerBase
{
    [HttpPost("brands/{brandId:guid}/metrics")]
    public async Task<EndpointResult<MetricResponse>> Create(
        Guid brandId,
        CreateMetricRequest request,
        [FromServices] ICommandHandler<MetricResponse, CreateMetricCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<MetricResponse>();

        var command = new CreateMetricCommand(brandId, userIdResult.Value, request);

        return EndpointResult<MetricResponse>.Created(
            await handler.Handle(command, cancellationToken));
    }

    [HttpGet("brands/{brandId:guid}/metrics")]
    public async Task<EndpointResult<IReadOnlyCollection<MetricResponse>>> GetByBrand(
        Guid brandId,
        [FromServices] IQueryHandler<IReadOnlyCollection<MetricResponse>, GetBrandManageMetricsQuery> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<IReadOnlyCollection<MetricResponse>>();

        return await handler.Handle(
            new GetBrandManageMetricsQuery(userIdResult.Value, brandId),
            cancellationToken);
    }

    [HttpGet("brands/{brandId:guid}/metrics/issue-options")]
    public async Task<EndpointResult<IReadOnlyCollection<MetricResponse>>> GetIssueOptions(
        Guid brandId,
        [FromServices] IQueryHandler<IReadOnlyCollection<MetricResponse>, GetBrandIssueMetricsQuery> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<IReadOnlyCollection<MetricResponse>>();

        return await handler.Handle(
            new GetBrandIssueMetricsQuery(userIdResult.Value, brandId),
            cancellationToken);
    }

    [HttpGet("brands/{brandId:guid}/metrics/redeem-options")]
    public async Task<EndpointResult<RedeemMetricOptionsResponse>> GetRedeemOptions(
        Guid brandId,
        [FromQuery] string redemptionCode,
        [FromServices] IQueryHandler<RedeemMetricOptionsResponse, GetRedeemMetricOptionsQuery> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<RedeemMetricOptionsResponse>();

        return await handler.Handle(
            new GetRedeemMetricOptionsQuery(userIdResult.Value, brandId, redemptionCode),
            cancellationToken);
    }

    [HttpGet("metrics/{metricDefinitionId:guid}")]
    public async Task<EndpointResult<MetricResponse>> GetDetails(
        Guid metricDefinitionId,
        [FromServices] IQueryHandler<MetricResponse, GetMetricDetailsQuery> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<MetricResponse>();

        return await handler.Handle(
            new GetMetricDetailsQuery(userIdResult.Value, metricDefinitionId),
            cancellationToken);
    }

    [HttpPut("metrics/{metricDefinitionId:guid}")]
    public async Task<EndpointResult<MetricResponse>> Update(
        Guid metricDefinitionId,
        UpdateMetricRequest request,
        [FromServices] ICommandHandler<MetricResponse, UpdateMetricCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<MetricResponse>();

        return await handler.Handle(
            new UpdateMetricCommand(metricDefinitionId, userIdResult.Value, request),
            cancellationToken);
    }

    [HttpPost("metrics/{metricDefinitionId:guid}/issue")]
    public async Task<EndpointResult<IssueMetricResponse>> Issue(
        Guid metricDefinitionId,
        IssueMetricRequest request,
        [FromServices] ICommandHandler<IssueMetricResponse, IssueMetricCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<IssueMetricResponse>();

        var command = new IssueMetricCommand(
            metricDefinitionId,
            userIdResult.Value,
            request);

        return await handler.Handle(command, cancellationToken);
    }

    [HttpPost("metrics/{metricDefinitionId:guid}/issue-by-customer-code")]
    public async Task<EndpointResult<IssueMetricResponse>> IssueByCustomerCode(
        Guid metricDefinitionId,
        IssueMetricByCustomerCodeRequest request,
        [FromServices] IRecipientResolver recipientResolver,
        [FromServices] ICommandHandler<IssueMetricResponse, IssueMetricCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<IssueMetricResponse>();

        var recipientResult = await recipientResolver.ResolveAsync(
            request.CustomerCode,
            cancellationToken);

        if (recipientResult.IsFailed)
            return Result.Fail<IssueMetricResponse>(recipientResult.Errors);

        var command = new IssueMetricCommand(
            metricDefinitionId,
            userIdResult.Value,
            new IssueMetricRequest(
                recipientResult.Value.UserId,
                request.Amount,
                string.IsNullOrWhiteSpace(request.Comment) ? "Issue metric" : request.Comment.Trim()));

        return await handler.Handle(command, cancellationToken);
    }

    [HttpPost("metrics/{metricDefinitionId:guid}/issue-by-phone")]
    public async Task<EndpointResult<IssueMetricResponse>> IssueByPhone(
        Guid metricDefinitionId,
        IssueMetricByPhoneRequest request,
        [FromServices] IRecipientResolver recipientResolver,
        [FromServices] ICommandHandler<IssueMetricResponse, IssueMetricCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<IssueMetricResponse>();

        var recipientResult = await recipientResolver.ResolveByPhoneAsync(
            request.PhoneNumber,
            cancellationToken);

        if (recipientResult.IsFailed)
            return Result.Fail<IssueMetricResponse>(recipientResult.Errors);

        var command = new IssueMetricCommand(
            metricDefinitionId,
            userIdResult.Value,
            new IssueMetricRequest(
                recipientResult.Value.UserId,
                request.Amount,
                string.IsNullOrWhiteSpace(request.Comment) ? "Issue metric" : request.Comment.Trim()));

        return await handler.Handle(command, cancellationToken);
    }

    [HttpPost("metrics/{metricDefinitionId:guid}/redeem")]
    public async Task<EndpointResult<RedeemMetricResponse>> Redeem(
        Guid metricDefinitionId,
        RedeemMetricRequest request,
        [FromServices] ICommandHandler<RedeemMetricResponse, RedeemMetricCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<RedeemMetricResponse>();

        var command = new RedeemMetricCommand(
            metricDefinitionId,
            userIdResult.Value,
            request);

        return await handler.Handle(command, cancellationToken);
    }

    [HttpGet("metrics/{metricDefinitionId:guid}/balances/{userId:guid}")]
    public async Task<EndpointResult<MetricBalanceResponse>> GetBalance(
        Guid metricDefinitionId,
        Guid userId,
        [FromServices] IQueryHandler<MetricBalanceResponse, GetMetricBalanceQuery> handler,
        CancellationToken cancellationToken)
    {
        var requestUserIdResult = GetUserId();
        if (requestUserIdResult.IsFailed)
            return requestUserIdResult.ToResult<MetricBalanceResponse>();

        var query = new GetMetricBalanceQuery(
            metricDefinitionId,
            userId,
            requestUserIdResult.Value);

        return await handler.Handle(query, cancellationToken);
    }

    [HttpGet("metrics/{metricDefinitionId:guid}/transactions")]
    public async Task<EndpointResult<MetricTransactionsResponse>> GetTransactions(
        Guid metricDefinitionId,
        [FromQuery] Guid userId,
        [FromQuery] int? skip,
        [FromQuery] int? take,
        [FromServices] IQueryHandler<MetricTransactionsResponse, GetMetricTransactionsQuery> handler,
        CancellationToken cancellationToken)
    {
        var requestUserIdResult = GetUserId();
        if (requestUserIdResult.IsFailed)
            return requestUserIdResult.ToResult<MetricTransactionsResponse>();

        var query = new GetMetricTransactionsQuery(
            metricDefinitionId,
            userId,
            requestUserIdResult.Value,
            skip ?? 0,
            take ?? 50);

        return await handler.Handle(query, cancellationToken);
    }

}
