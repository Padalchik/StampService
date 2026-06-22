using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Audit;
using StampService.Application.Errors;
using StampService.Application.Metrics;
using StampService.Contracts.DTOs.Brands;
using StampService.Domain.Access;

namespace StampService.Application.Brands.Commands.UpdateBrandRewardSettings;

public class UpdateBrandRewardSettingsHandler : ICommandHandler<UpdateBrandResponse, UpdateBrandRewardSettingsCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandRepository _brandRepository;
    private readonly IBusinessAuditSink _businessAuditSink;
    private readonly ILoyaltyMetricRepository? _metricRepository;

    public UpdateBrandRewardSettingsHandler(
        IBrandAccessService brandAccessService,
        IBrandRepository brandRepository,
        ILoyaltyMetricRepository? metricRepository = null,
        IBusinessAuditSink? businessAuditSink = null)
    {
        _brandAccessService = brandAccessService;
        _brandRepository = brandRepository;
        _businessAuditSink = businessAuditSink ?? NoopBusinessAuditSink.Instance;
        _metricRepository = metricRepository;
    }

    public async Task<Result<UpdateBrandResponse>> Handle(
        UpdateBrandRewardSettingsCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ActorUserId == Guid.Empty)
            return await RejectedAsync(command, [UserErrors.IdIsEmpty()], cancellationToken);

        if (command.BrandId == Guid.Empty)
            return await RejectedAsync(command, [BrandErrors.IdIsEmpty()], cancellationToken);

        var canManageBrand = await _brandAccessService.CanAsync(
            command.ActorUserId,
            command.BrandId,
            PermissionCode.BrandManage,
            cancellationToken);

        if (!canManageBrand)
            return await RejectedAsync(command, [AccessErrors.Denied()], cancellationToken);

        var brand = await _brandRepository.GetByIdForUpdateAsync(command.BrandId, cancellationToken);
        if (brand is null)
            return await RejectedAsync(command, [BrandErrors.NotFound()], cancellationToken);

        if (command.IsWelcomeRewardsEnabled.HasValue)
        {
            var preValidationResult = await ValidateWelcomeSettingsBeforeMutationAsync(
                command,
                cancellationToken);
            if (preValidationResult.IsFailed)
                return await RejectedAsync(command, preValidationResult.Errors, cancellationToken);
        }

        var updateResult = brand.UpdateDetails(
            brand.Name,
            command.IsMetricsEnabled,
            command.IsCoinsEnabled,
            command.IsCoinProductRedemptionEnabled,
            command.IsManualCoinRedemptionEnabled);

        if (updateResult.IsFailed)
            return await RejectedAsync(command, updateResult.Errors, cancellationToken);

        if (command.IsWelcomeRewardsEnabled.HasValue)
        {
            var metrics = command.WelcomeMetrics ?? Array.Empty<Domain.Brand.BrandWelcomeMetricRewardSetting>();
            var welcomeUpdateResult = brand.UpdateWelcomeRewardSettings(
                command.IsWelcomeRewardsEnabled.Value,
                metrics,
                command.WelcomeCoinsAmount,
                command.WelcomeRewardComment);
            if (welcomeUpdateResult.IsFailed)
                return await RejectedAsync(command, welcomeUpdateResult.Errors, cancellationToken);
        }

        await _brandRepository.SaveAsync(cancellationToken);
        await _businessAuditSink.RecordAsync(
            new BusinessAuditEvent(
                BusinessAuditOperationType.UpdateRewardSettings,
                BusinessAuditOperationStatus.Succeeded,
                BrandId: command.BrandId,
                ActorUserId: command.ActorUserId,
                TargetEntityType: BusinessAuditTargetEntityType.Brand,
                TargetEntityId: brand.Id,
                Metadata: CreateSettingsMetadata(command)),
            cancellationToken);

        return Result.Ok(new UpdateBrandResponse(
            brand.Id,
            brand.Name,
            brand.IsMetricsEnabled,
            brand.IsCoinsEnabled,
            brand.IsCoinProductRedemptionEnabled,
            brand.IsManualCoinRedemptionEnabled,
            new BrandWelcomeRewardSettingsResponse(
                brand.IsWelcomeRewardsEnabled,
                brand.WelcomeMetricRewards
                    .Select(reward => new BrandWelcomeMetricRewardResponse(
                        reward.MetricDefinitionId,
                        reward.Amount))
                    .ToArray(),
                brand.WelcomeCoinsAmount,
                brand.WelcomeRewardComment),
            brand.UpdatedAt));
    }

    private async Task<Result> ValidateWelcomeSettingsBeforeMutationAsync(
        UpdateBrandRewardSettingsCommand command,
        CancellationToken cancellationToken)
    {
        var requestedMetrics = command.WelcomeMetrics ?? Array.Empty<Domain.Brand.BrandWelcomeMetricRewardSetting>();
        if (!command.IsMetricsEnabled && requestedMetrics.Count > 0)
            return Result.Fail(AppError.Validation(
                AppErrorCodes.Validation.ValueInvalid,
                "Welcome metric rewards cannot be enabled when metrics are disabled",
                nameof(command.WelcomeMetrics)));

        if (requestedMetrics.Any(metric => metric.MetricDefinitionId == Guid.Empty || metric.Amount <= 0))
            return Result.Fail(AppError.Validation(
                AppErrorCodes.Validation.ValueInvalid,
                "Welcome metric rewards must have a metric and positive amount",
                nameof(command.WelcomeMetrics)));

        if (!command.IsCoinsEnabled && command.WelcomeCoinsAmount > 0)
            return Result.Fail(AppError.Validation(
                AppErrorCodes.Validation.ValueInvalid,
                "Welcome coin rewards cannot be enabled when coins are disabled",
                nameof(command.WelcomeCoinsAmount)));

        if (command.WelcomeCoinsAmount < 0)
            return Result.Fail(AppError.Validation(
                AppErrorCodes.Validation.ValueInvalid,
                "Welcome coins amount cannot be negative",
                nameof(command.WelcomeCoinsAmount)));

        if (command.IsWelcomeRewardsEnabled == true
            && requestedMetrics.Count == 0
            && command.WelcomeCoinsAmount == 0)
            return Result.Fail(AppError.Validation(
                AppErrorCodes.Validation.ValueInvalid,
                "At least one welcome reward must be configured",
                nameof(command.IsWelcomeRewardsEnabled)));

        if (!string.IsNullOrWhiteSpace(command.WelcomeRewardComment)
            && command.WelcomeRewardComment.Trim().Length > 200)
            return Result.Fail(AppError.Validation(
                AppErrorCodes.Validation.ValueInvalid,
                "Welcome reward comment cannot exceed 200 characters",
                nameof(command.WelcomeRewardComment)));

        if (requestedMetrics.Count == 0 || _metricRepository is null)
            return Result.Ok();

        var brandMetrics = await _metricRepository.GetByBrandAsync(command.BrandId, cancellationToken);
        var activeMetricIds = brandMetrics
            .Where(metric => metric.IsActive)
            .Select(metric => metric.Id)
            .ToHashSet();

        return requestedMetrics.Select(metric => metric.MetricDefinitionId).All(activeMetricIds.Contains)
            ? Result.Ok()
            : Result.Fail(MetricErrors.NotFound());
    }

    private async Task<Result<UpdateBrandResponse>> RejectedAsync(
        UpdateBrandRewardSettingsCommand command,
        IReadOnlyCollection<IError> errors,
        CancellationToken cancellationToken)
    {
        await _businessAuditSink.RecordAsync(
            new BusinessAuditEvent(
                BusinessAuditOperationType.UpdateRewardSettings,
                BusinessAuditOperationStatus.Rejected,
                BrandId: command.BrandId == Guid.Empty ? null : command.BrandId,
                ActorUserId: command.ActorUserId == Guid.Empty ? null : command.ActorUserId,
                TargetEntityType: BusinessAuditTargetEntityType.Brand,
                TargetEntityId: command.BrandId == Guid.Empty ? null : command.BrandId,
                ReasonCode: BusinessAuditReason.FromErrors(errors),
                Metadata: CreateSettingsMetadata(command)),
            cancellationToken);

        return Result.Fail(errors);
    }

    private static IReadOnlyDictionary<string, object?> CreateSettingsMetadata(
        UpdateBrandRewardSettingsCommand command)
    {
        return new Dictionary<string, object?>
        {
            ["isMetricsEnabled"] = command.IsMetricsEnabled,
            ["isCoinsEnabled"] = command.IsCoinsEnabled,
            ["isCoinProductRedemptionEnabled"] = command.IsCoinProductRedemptionEnabled,
            ["isManualCoinRedemptionEnabled"] = command.IsManualCoinRedemptionEnabled,
            ["isWelcomeRewardsEnabled"] = command.IsWelcomeRewardsEnabled,
            ["welcomeMetrics"] = command.WelcomeMetrics,
            ["welcomeCoinsAmount"] = command.WelcomeCoinsAmount
        };
    }
}
