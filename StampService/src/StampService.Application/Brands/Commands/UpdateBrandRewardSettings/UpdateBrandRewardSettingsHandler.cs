using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.Brands;
using StampService.Domain.Access;

namespace StampService.Application.Brands.Commands.UpdateBrandRewardSettings;

public class UpdateBrandRewardSettingsHandler : ICommandHandler<UpdateBrandResponse, UpdateBrandRewardSettingsCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandRepository _brandRepository;

    public UpdateBrandRewardSettingsHandler(
        IBrandAccessService brandAccessService,
        IBrandRepository brandRepository)
    {
        _brandAccessService = brandAccessService;
        _brandRepository = brandRepository;
    }

    public async Task<Result<UpdateBrandResponse>> Handle(
        UpdateBrandRewardSettingsCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ActorUserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        if (command.BrandId == Guid.Empty)
            return Result.Fail(BrandErrors.IdIsEmpty());

        var canManageBrand = await _brandAccessService.CanAsync(
            command.ActorUserId,
            command.BrandId,
            PermissionCode.BrandManage,
            cancellationToken);

        if (!canManageBrand)
            return Result.Fail(AccessErrors.Denied());

        var brand = await _brandRepository.GetByIdForUpdateAsync(command.BrandId, cancellationToken);
        if (brand is null)
            return Result.Fail(BrandErrors.NotFound());

        var updateResult = brand.UpdateDetails(
            brand.Name,
            command.IsMetricsEnabled,
            command.IsCoinsEnabled,
            command.IsCoinProductRedemptionEnabled,
            command.IsManualCoinRedemptionEnabled);

        if (updateResult.IsFailed)
            return Result.Fail(updateResult.Errors);

        await _brandRepository.SaveAsync(cancellationToken);

        return Result.Ok(new UpdateBrandResponse(
            brand.Id,
            brand.Name,
            brand.IsMetricsEnabled,
            brand.IsCoinsEnabled,
            brand.IsCoinProductRedemptionEnabled,
            brand.IsManualCoinRedemptionEnabled,
            brand.UpdatedAt));
    }
}
