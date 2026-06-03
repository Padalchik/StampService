using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Audit;
using StampService.Application.CustomerNotifications;
using StampService.Application.Users.Commands.UseRedemptionCode;
using StampService.Contracts.DTOs.Metrics;

namespace StampService.Application.Metrics.Commands.RedeemMetric;

public class RedeemMetricHandler : ICommandHandler<RedeemMetricResponse, RedeemMetricCommand>
{
    private readonly IMetricLedgerService _metricLedgerService;
    private readonly IBusinessAuditSink _businessAuditSink;
    private readonly ICustomerNotificationService _customerNotificationService;
    private readonly IRedeemMetricValidationService _redeemMetricValidationService;
    private readonly ICommandHandler<UseRedemptionCodeResponse, UseRedemptionCodeCommand> _useRedemptionCodeHandler;

    public RedeemMetricHandler(
        IMetricLedgerService metricLedgerService,
        IRedeemMetricValidationService redeemMetricValidationService,
        ICommandHandler<UseRedemptionCodeResponse, UseRedemptionCodeCommand> useRedemptionCodeHandler,
        ICustomerNotificationService? customerNotificationService = null,
        IBusinessAuditSink? businessAuditSink = null)
    {
        _metricLedgerService = metricLedgerService;
        _businessAuditSink = businessAuditSink ?? NoopBusinessAuditSink.Instance;
        _customerNotificationService = customerNotificationService ?? NullCustomerNotificationService.Instance;
        _redeemMetricValidationService = redeemMetricValidationService;
        _useRedemptionCodeHandler = useRedemptionCodeHandler;
    }

    public async Task<Result<RedeemMetricResponse>> Handle(
        RedeemMetricCommand command,
        CancellationToken cancellationToken)
    {
        var precheckResult = await _redeemMetricValidationService.ValidateAsync(
            command.MetricDefinitionId,
            command.RedeemerUserId,
            command.Request.RedemptionCode,
            cancellationToken);

        if (precheckResult.IsFailed)
            return await RejectedAsync(command, precheckResult.Errors, null, null, null, cancellationToken);

        var metric = precheckResult.Value.Metric;
        var useCodeResult = await _useRedemptionCodeHandler.Handle(
            new UseRedemptionCodeCommand(command.Request.RedemptionCode),
            cancellationToken);

        if (useCodeResult.IsFailed)
            return await RejectedAsync(
                command,
                useCodeResult.Errors,
                metric.BrandId,
                precheckResult.Value.CustomerUserId,
                precheckResult.Value.CurrentBalanceValue,
                cancellationToken);

        var ledgerResult = await _metricLedgerService.RedeemAsync(
            useCodeResult.Value.UserId,
            command.RedeemerUserId,
            metric.BrandId,
            command.MetricDefinitionId,
            metric.RedemptionAmount,
            command.Request.Comment,
            cancellationToken);

        if (ledgerResult.IsFailed)
            return await RejectedAsync(
                command,
                ledgerResult.Errors,
                metric.BrandId,
                precheckResult.Value.CustomerUserId,
                precheckResult.Value.CurrentBalanceValue,
                cancellationToken);

        var balance = ledgerResult.Value.Balance;
        var transaction = ledgerResult.Value.Transaction;

        var response = new RedeemMetricResponse(
            transaction.Id,
            balance.Id,
            balance.BrandId,
            balance.MetricDefinitionId,
            balance.UserId,
            transaction.Type.ToString(),
            transaction.Amount,
            balance.Value,
            transaction.CreatedAt);

        await _customerNotificationService.NotifyMetricRedeemedAsync(response, cancellationToken);
        await _businessAuditSink.RecordAsync(
            new BusinessAuditEvent(
                BusinessAuditOperationType.RedeemMetric,
                BusinessAuditOperationStatus.Succeeded,
                BrandId: balance.BrandId,
                ActorUserId: command.RedeemerUserId,
                CustomerUserId: balance.UserId,
                TargetEntityType: BusinessAuditTargetEntityType.MetricDefinition,
                TargetEntityId: balance.MetricDefinitionId,
                Amount: transaction.Amount,
                BalanceBefore: ledgerResult.Value.BalanceBefore,
                BalanceAfter: ledgerResult.Value.BalanceAfter,
                Comment: command.Request.Comment,
                Metadata: new Dictionary<string, object?>
                {
                    ["stampTransactionId"] = transaction.Id
                }),
            cancellationToken);

        return Result.Ok(response);
    }

    private async Task<Result<RedeemMetricResponse>> RejectedAsync(
        RedeemMetricCommand command,
        IReadOnlyCollection<IError> errors,
        Guid? brandId,
        Guid? customerUserId,
        int? balanceBefore,
        CancellationToken cancellationToken)
    {
        await _businessAuditSink.RecordAsync(
            new BusinessAuditEvent(
                BusinessAuditOperationType.RedeemMetric,
                BusinessAuditOperationStatus.Rejected,
                BrandId: brandId,
                ActorUserId: command.RedeemerUserId,
                CustomerUserId: customerUserId,
                TargetEntityType: BusinessAuditTargetEntityType.MetricDefinition,
                TargetEntityId: command.MetricDefinitionId,
                BalanceBefore: balanceBefore,
                ReasonCode: BusinessAuditReason.FromErrors(errors),
                Comment: command.Request.Comment),
            cancellationToken);

        return Result.Fail(errors);
    }
}
