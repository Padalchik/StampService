using StampService.Application.Abstractions;

namespace StampService.Application.Brands.Commands.UpdateBrandRewardSettings;

public record UpdateBrandRewardSettingsCommand(
    Guid ActorUserId,
    Guid BrandId,
    bool IsMetricsEnabled,
    bool IsCoinsEnabled) : ICommand;
