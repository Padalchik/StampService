using StampService.Application.Abstractions;
using StampService.Domain.Brand;

namespace StampService.Application.Brands.Commands.UpdateBrandRewardSettings;

public record UpdateBrandRewardSettingsCommand(
    Guid ActorUserId,
    Guid BrandId,
    bool IsMetricsEnabled,
    bool IsCoinsEnabled,
    bool IsCoinProductRedemptionEnabled,
    bool IsManualCoinRedemptionEnabled,
    IReadOnlyCollection<BrandWelcomeMetricRewardSetting>? WelcomeMetrics = null,
    int WelcomeCoinsAmount = 0,
    string? WelcomeRewardComment = null,
    bool? IsWelcomeRewardsEnabled = null) : ICommand;
