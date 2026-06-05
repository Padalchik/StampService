using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Audit;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.Brands;
using StampService.Domain.Access;

namespace StampService.Application.Brands.Commands.UpdateBrandRewardSettings;

public class UpdateBrandRewardSettingsHandler : ICommandHandler<UpdateBrandResponse, UpdateBrandRewardSettingsCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandRepository _brandRepository;
    private readonly IBusinessAuditSink _businessAuditSink;

    public UpdateBrandRewardSettingsHandler(
        IBrandAccessService brandAccessService,
        IBrandRepository brandRepository,
        IBusinessAuditSink? businessAuditSink = null)
    {
        _brandAccessService = brandAccessService;
        _brandRepository = brandRepository;
        _businessAuditSink = businessAuditSink ?? NoopBusinessAuditSink.Instance;
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

        var updateResult = brand.UpdateDetails(
            brand.Name,
            command.IsMetricsEnabled,
            command.IsCoinsEnabled,
            command.IsCoinProductRedemptionEnabled,
            command.IsManualCoinRedemptionEnabled);

        if (updateResult.IsFailed)
            return await RejectedAsync(command, updateResult.Errors, cancellationToken);

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
            brand.UpdatedAt));
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
            ["isManualCoinRedemptionEnabled"] = command.IsManualCoinRedemptionEnabled
        };
    }
}
