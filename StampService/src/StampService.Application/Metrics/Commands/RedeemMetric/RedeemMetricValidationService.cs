using FluentResults;
using StampService.Application.Access;
using StampService.Application.Brands;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Domain.Access;
using StampService.Domain.User;

namespace StampService.Application.Metrics.Commands.RedeemMetric;

public class RedeemMetricValidationService : IRedeemMetricValidationService
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandRepository _brandRepository;
    private readonly ILoyaltyMetricRepository _metricRepository;
    private readonly IRedemptionCodeRepository _redemptionCodeRepository;
    private readonly IMetricBalanceRepository _metricBalanceRepository;
    private readonly IStampTransactionRepository _stampTransactionRepository;
    private readonly TimeProvider _timeProvider;

    public RedeemMetricValidationService(
        IBrandAccessService brandAccessService,
        IBrandRepository brandRepository,
        ILoyaltyMetricRepository metricRepository,
        IRedemptionCodeRepository redemptionCodeRepository,
        IMetricBalanceRepository metricBalanceRepository,
        IStampTransactionRepository stampTransactionRepository,
        TimeProvider timeProvider)
    {
        _brandAccessService = brandAccessService;
        _brandRepository = brandRepository;
        _metricRepository = metricRepository;
        _redemptionCodeRepository = redemptionCodeRepository;
        _metricBalanceRepository = metricBalanceRepository;
        _stampTransactionRepository = stampTransactionRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<RedeemMetricPrecheckResult>> ValidateAsync(
        Guid metricDefinitionId,
        Guid redeemerUserId,
        string redemptionCode,
        CancellationToken cancellationToken)
    {
        var metric = await _metricRepository.GetByIdAsync(metricDefinitionId, cancellationToken);
        if (metric is null)
            return Result.Fail(MetricErrors.NotFound());

        var brand = await _brandRepository.GetByIdAsync(metric.BrandId, cancellationToken);
        if (brand is null)
            return Result.Fail(BrandErrors.NotFound());

        if (!brand.IsMetricsEnabled)
            return Result.Fail(BrandErrors.MetricsDisabled());

        var canRedeem = await _brandAccessService.CanAsync(
            redeemerUserId,
            metric.BrandId,
            PermissionCode.StampRedeem,
            cancellationToken);

        if (!canRedeem)
            return Result.Fail(AccessErrors.Denied());

        if (!metric.IsActive)
            return Result.Fail(MetricErrors.IsNotActive());

        var code = redemptionCode?.Trim() ?? string.Empty;
        if (!RedemptionCode.IsValidCode(code))
            return Result.Fail(UserErrors.RedemptionCodeInvalid());

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var activeCode = await _redemptionCodeRepository.GetActiveByCodeAsync(
            code,
            nowUtc,
            cancellationToken);

        if (activeCode is null)
            return Result.Fail(UserErrors.RedemptionCodeNotFoundOrExpired());

        var balance = await _metricBalanceRepository.GetByUserAndMetricAsync(
            activeCode.UserId,
            metric.BrandId,
            metric.Id,
            cancellationToken);

        if (balance is null)
            return Result.Fail(MetricErrors.BalanceNotFound());

        var currentBalanceValue = await _stampTransactionRepository.CalculateMetricBalanceValueAsync(
            balance.Id,
            cancellationToken);

        if (currentBalanceValue < metric.RedemptionAmount)
            return Result.Fail(MetricErrors.InsufficientFunds(currentBalanceValue, metric.RedemptionAmount));

        return Result.Ok(new RedeemMetricPrecheckResult(
            metric,
            activeCode.UserId,
            currentBalanceValue));
    }
}
