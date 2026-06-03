using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Audit;
using StampService.Application.Brands;
using StampService.Application.CustomerNotifications;
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
    private readonly IBusinessAuditSink _businessAuditSink;
    private readonly ICustomerNotificationService _customerNotificationService;
    private readonly IMetricLedgerService _metricLedgerService;
    private readonly ILoyaltyMetricRepository _metricRepository;
    private readonly IPhoneAccountService _phoneAccountService;

    public IssueMetricByPhoneHandler(
        IBrandAccessService brandAccessService,
        IBrandRepository brandRepository,
        IMetricLedgerService metricLedgerService,
        ILoyaltyMetricRepository metricRepository,
        IPhoneAccountService phoneAccountService,
        ICustomerNotificationService? customerNotificationService = null,
        IBusinessAuditSink? businessAuditSink = null)
    {
        _brandAccessService = brandAccessService;
        _brandRepository = brandRepository;
        _businessAuditSink = businessAuditSink ?? NoopBusinessAuditSink.Instance;
        _customerNotificationService = customerNotificationService ?? NullCustomerNotificationService.Instance;
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
            return await RejectedAsync(command, [MetricErrors.NotFound()], null, null, null, null, cancellationToken);

        var brand = await _brandRepository.GetByIdAsync(metric.BrandId, cancellationToken);
        if (brand is null)
            return await RejectedAsync(command, [BrandErrors.NotFound()], metric.BrandId, null, null, null, cancellationToken);

        if (!brand.IsMetricsEnabled)
            return await RejectedAsync(command, [BrandErrors.MetricsDisabled()], metric.BrandId, null, null, null, cancellationToken);

        var canIssue = await _brandAccessService.CanAsync(
            command.IssuerUserId,
            metric.BrandId,
            PermissionCode.StampIssue,
            cancellationToken);

        if (!canIssue)
            return await RejectedAsync(command, [AccessErrors.Denied()], metric.BrandId, null, null, null, cancellationToken);

        if (!metric.IsActive)
            return await RejectedAsync(command, [MetricErrors.IsNotActive()], metric.BrandId, null, null, null, cancellationToken);

        var comment = string.IsNullOrWhiteSpace(command.Request.Comment)
            ? "Issue metric"
            : command.Request.Comment.Trim();
        var transactionValidation = StampTransaction.CreateIssue(
            Guid.NewGuid(),
            command.Request.Amount,
            comment,
            command.IssuerUserId);
        if (transactionValidation.IsFailed)
            return await RejectedAsync(command, transactionValidation.Errors, metric.BrandId, null, null, comment, cancellationToken);

        var customerResult = await _phoneAccountService.GetOrCreateForBusinessOperationAsync(
            command.Request.PhoneNumber,
            nameof(command.Request.PhoneNumber),
            cancellationToken);
        if (customerResult.IsFailed)
            return await RejectedAsync(command, customerResult.Errors, metric.BrandId, null, null, comment, cancellationToken);

        var ledgerResult = await _metricLedgerService.IssueAsync(
            customerResult.Value.Id,
            command.IssuerUserId,
            metric.BrandId,
            command.MetricDefinitionId,
            command.Request.Amount,
            comment,
            cancellationToken);

        if (ledgerResult.IsFailed)
            return await RejectedAsync(command, ledgerResult.Errors, metric.BrandId, customerResult.Value.Id, null, comment, cancellationToken);

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

        await _customerNotificationService.NotifyMetricIssuedAsync(response, cancellationToken);
        await _businessAuditSink.RecordAsync(
            new BusinessAuditEvent(
                BusinessAuditOperationType.IssueMetric,
                BusinessAuditOperationStatus.Succeeded,
                BrandId: balance.BrandId,
                ActorUserId: command.IssuerUserId,
                CustomerUserId: balance.UserId,
                TargetEntityType: BusinessAuditTargetEntityType.MetricDefinition,
                TargetEntityId: balance.MetricDefinitionId,
                Amount: transaction.Amount,
                BalanceBefore: ledgerResult.Value.BalanceBefore,
                BalanceAfter: ledgerResult.Value.BalanceAfter,
                Comment: comment,
                Metadata: new Dictionary<string, object?>
                {
                    ["stampTransactionId"] = transaction.Id
                }),
            cancellationToken);

        return Result.Ok(response);
    }

    private async Task<Result<IssueMetricResponse>> RejectedAsync(
        IssueMetricByPhoneCommand command,
        IReadOnlyCollection<IError> errors,
        Guid? brandId,
        Guid? customerUserId,
        int? balanceBefore,
        string? comment,
        CancellationToken cancellationToken)
    {
        await _businessAuditSink.RecordAsync(
            new BusinessAuditEvent(
                BusinessAuditOperationType.IssueMetric,
                BusinessAuditOperationStatus.Rejected,
                BrandId: brandId,
                ActorUserId: command.IssuerUserId,
                CustomerUserId: customerUserId,
                TargetEntityType: BusinessAuditTargetEntityType.MetricDefinition,
                TargetEntityId: command.MetricDefinitionId,
                Amount: command.Request.Amount,
                BalanceBefore: balanceBefore,
                ReasonCode: BusinessAuditReason.FromErrors(errors),
                Comment: comment),
            cancellationToken);

        return Result.Fail(errors);
    }
}
