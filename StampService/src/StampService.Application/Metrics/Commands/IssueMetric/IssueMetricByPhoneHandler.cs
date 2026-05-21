using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Brands;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Access;
using StampService.Domain.Loyalty;

namespace StampService.Application.Metrics.Commands.IssueMetric;

public class IssueMetricByPhoneHandler : ICommandHandler<IssueMetricResponse, IssueMetricByPhoneCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandRepository _brandRepository;
    private readonly IMetricLedgerService _metricLedgerService;
    private readonly ILoyaltyMetricRepository _metricRepository;
    private readonly IPhoneAccountService _phoneAccountService;

    public IssueMetricByPhoneHandler(
        IBrandAccessService brandAccessService,
        IBrandRepository brandRepository,
        IMetricLedgerService metricLedgerService,
        ILoyaltyMetricRepository metricRepository,
        IPhoneAccountService phoneAccountService)
    {
        _brandAccessService = brandAccessService;
        _brandRepository = brandRepository;
        _metricLedgerService = metricLedgerService;
        _metricRepository = metricRepository;
        _phoneAccountService = phoneAccountService;
    }

    public async Task<Result<IssueMetricResponse>> Handle(
        IssueMetricByPhoneCommand command,
        CancellationToken cancellationToken)
    {
        var metric = await _metricRepository.GetByIdAsync(
            command.MetricDefinitionId,
            cancellationToken);

        if (metric is null)
            return Result.Fail(MetricErrors.NotFound());

        var brand = await _brandRepository.GetByIdAsync(metric.BrandId, cancellationToken);
        if (brand is null)
            return Result.Fail(BrandErrors.NotFound());

        if (!brand.IsMetricsEnabled)
            return Result.Fail(BrandErrors.MetricsDisabled());

        var canIssue = await _brandAccessService.CanAsync(
            command.IssuerUserId,
            metric.BrandId,
            PermissionCode.StampIssue,
            cancellationToken);

        if (!canIssue)
            return Result.Fail(AccessErrors.Denied());

        if (!metric.IsActive)
            return Result.Fail(MetricErrors.IsNotActive());

        var comment = string.IsNullOrWhiteSpace(command.Request.Comment)
            ? "Issue metric"
            : command.Request.Comment.Trim();
        var transactionValidation = StampTransaction.CreateIssue(
            Guid.NewGuid(),
            command.Request.Amount,
            comment,
            command.IssuerUserId);
        if (transactionValidation.IsFailed)
            return Result.Fail(transactionValidation.Errors);

        var customerResult = await _phoneAccountService.GetOrCreateForBusinessOperationAsync(
            command.Request.PhoneNumber,
            nameof(command.Request.PhoneNumber),
            cancellationToken);
        if (customerResult.IsFailed)
            return Result.Fail(customerResult.Errors);

        var ledgerResult = await _metricLedgerService.IssueAsync(
            customerResult.Value.Id,
            command.IssuerUserId,
            metric.BrandId,
            command.MetricDefinitionId,
            command.Request.Amount,
            comment,
            cancellationToken);

        if (ledgerResult.IsFailed)
            return Result.Fail(ledgerResult.Errors);

        var balance = ledgerResult.Value.Balance;
        var transaction = ledgerResult.Value.Transaction;

        var response = new IssueMetricResponse(
            transaction.Id,
            balance.Id,
            balance.BrandId,
            balance.MetricDefinitionId,
            balance.UserId,
            transaction.Type.ToString(),
            transaction.Amount,
            balance.Value,
            transaction.CreatedAt);

        return Result.Ok(response);
    }
}
