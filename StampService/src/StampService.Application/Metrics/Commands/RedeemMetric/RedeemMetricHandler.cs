using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Users.Commands.UseRedemptionCode;
using StampService.Contracts.DTOs.Metrics;

namespace StampService.Application.Metrics.Commands.RedeemMetric;

public class RedeemMetricHandler : ICommandHandler<RedeemMetricResponse, RedeemMetricCommand>
{
    private readonly IMetricLedgerService _metricLedgerService;
    private readonly IRedeemMetricValidationService _redeemMetricValidationService;
    private readonly ICommandHandler<UseRedemptionCodeResponse, UseRedemptionCodeCommand> _useRedemptionCodeHandler;

    public RedeemMetricHandler(
        IMetricLedgerService metricLedgerService,
        IRedeemMetricValidationService redeemMetricValidationService,
        ICommandHandler<UseRedemptionCodeResponse, UseRedemptionCodeCommand> useRedemptionCodeHandler)
    {
        _metricLedgerService = metricLedgerService;
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
            return Result.Fail(precheckResult.Errors);

        var useCodeResult = await _useRedemptionCodeHandler.Handle(
            new UseRedemptionCodeCommand(command.Request.RedemptionCode),
            cancellationToken);

        if (useCodeResult.IsFailed)
            return Result.Fail(useCodeResult.Errors);

        var metric = precheckResult.Value.Metric;
        var ledgerResult = await _metricLedgerService.RedeemAsync(
            useCodeResult.Value.UserId,
            metric.BrandId,
            command.MetricDefinitionId,
            metric.RedemptionAmount,
            command.Request.Comment,
            cancellationToken);

        if (ledgerResult.IsFailed)
            return Result.Fail(ledgerResult.Errors);

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

        return Result.Ok(response);
    }
}
