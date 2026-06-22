using FluentResults;
using StampService.Domain.Shared;

namespace StampService.Domain.Brand;

public class Brand : BaseEntity
{
    private HashSet<Location> _locations = [];
    private List<BrandWelcomeMetricReward> _welcomeMetricRewards = [];
    
    public string Name { get; private set; }
    public bool IsMetricsEnabled { get; private set; }
    public bool IsCoinsEnabled { get; private set; }
    public bool IsCoinProductRedemptionEnabled { get; private set; }
    public bool IsManualCoinRedemptionEnabled { get; private set; }
    public bool IsWelcomeRewardsEnabled { get; private set; }
    public int WelcomeCoinsAmount { get; private set; }
    public string WelcomeRewardComment { get; private set; }
    
    public IReadOnlySet<Location> Locations => _locations;
    public IReadOnlyCollection<BrandWelcomeMetricReward> WelcomeMetricRewards => _welcomeMetricRewards;

    private Brand(string name)
    {
        Name = name;
        IsMetricsEnabled = true;
        IsCoinsEnabled = true;
        IsCoinProductRedemptionEnabled = true;
        IsManualCoinRedemptionEnabled = false;
        IsWelcomeRewardsEnabled = false;
        WelcomeCoinsAmount = 0;
        WelcomeRewardComment = "Приветственная награда";
    }
    
    // EF Core
    protected Brand()
    {
        Name = null!;
        WelcomeRewardComment = null!;
    }
    
    public static Result<Brand> Create(string name)
    {
        var validationResult = ValidateDetails(
            name,
            isMetricsEnabled: true,
            isCoinsEnabled: true,
            isCoinProductRedemptionEnabled: true,
            isManualCoinRedemptionEnabled: false);
        if (validationResult.IsFailed)
            return Result.Fail(validationResult.Errors);

        var brand = new Brand(name.Trim());
        return Result.Ok(brand);
    }

    public Result UpdateDetails(
        string name,
        bool isMetricsEnabled,
        bool isCoinsEnabled,
        bool isCoinProductRedemptionEnabled = true,
        bool isManualCoinRedemptionEnabled = false)
    {
        var validationResult = ValidateDetails(
            name,
            isMetricsEnabled,
            isCoinsEnabled,
            isCoinProductRedemptionEnabled,
            isManualCoinRedemptionEnabled);
        if (validationResult.IsFailed)
            return Result.Fail(validationResult.Errors);

        Name = name.Trim();
        IsMetricsEnabled = isMetricsEnabled;
        IsCoinsEnabled = isCoinsEnabled;
        IsCoinProductRedemptionEnabled = isCoinProductRedemptionEnabled;
        IsManualCoinRedemptionEnabled = isManualCoinRedemptionEnabled;
        if (!IsMetricsEnabled)
            _welcomeMetricRewards.Clear();
        if (!IsCoinsEnabled)
            WelcomeCoinsAmount = 0;
        if (IsWelcomeRewardsEnabled && _welcomeMetricRewards.Count == 0 && WelcomeCoinsAmount == 0)
            IsWelcomeRewardsEnabled = false;
        Touch();

        return Result.Ok();
    }

    public Result UpdateWelcomeRewardSettings(
        bool isWelcomeRewardsEnabled,
        IEnumerable<BrandWelcomeMetricRewardSetting> welcomeMetricRewards,
        int welcomeCoinsAmount,
        string? welcomeRewardComment = null)
    {
        var metricRewardItems = welcomeMetricRewards
            .Where(reward => reward.MetricDefinitionId != Guid.Empty)
            .ToArray();

        if (metricRewardItems.Any(reward => reward.Amount <= 0))
            return Result.Fail(DomainError.Validation(
                "brand.welcome_metric_amount_invalid",
                "Welcome metric amount must be positive",
                nameof(welcomeMetricRewards)));

        var metricRewardResults = metricRewardItems
            .GroupBy(reward => reward.MetricDefinitionId)
            .Select(group => BrandWelcomeMetricReward.Create(
                group.Key,
                group.Sum(reward => reward.Amount)))
            .ToArray();
        if (metricRewardResults.Any(result => result.IsFailed))
            return Result.Fail(metricRewardResults.SelectMany(result => result.Errors));

        var metricRewards = metricRewardResults
            .Select(result => result.Value)
            .ToArray();

        var validationResult = ValidateWelcomeRewardSettings(
            isWelcomeRewardsEnabled,
            metricRewards,
            welcomeCoinsAmount,
            welcomeRewardComment);
        if (validationResult.IsFailed)
            return Result.Fail(validationResult.Errors);

        IsWelcomeRewardsEnabled = isWelcomeRewardsEnabled;
        _welcomeMetricRewards = metricRewards.ToList();
        WelcomeCoinsAmount = welcomeCoinsAmount;
        WelcomeRewardComment = string.IsNullOrWhiteSpace(welcomeRewardComment)
            ? "Приветственная награда"
            : welcomeRewardComment.Trim();
        Touch();

        return Result.Ok();
    }

    private static Result ValidateDetails(
        string name,
        bool isMetricsEnabled,
        bool isCoinsEnabled,
        bool isCoinProductRedemptionEnabled,
        bool isManualCoinRedemptionEnabled)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail(DomainError.Validation(
                "brand.name_required",
                "Name не может быть пустым",
                nameof(name)));

        if (name.Length < Constants.MIN_BRAND_NAME_LENGTH || name.Length > Constants.MAX_BRAND_NAME_LENGTH)
            return Result.Fail(DomainError.Validation(
                "brand.name_length_invalid",
                $"Name должен быть от {Constants.MIN_BRAND_NAME_LENGTH} до {Constants.MAX_BRAND_NAME_LENGTH} символов",
                nameof(name)));

        if (!isMetricsEnabled && !isCoinsEnabled)
            return Result.Fail(DomainError.Validation(
                "brand.reward_types_required",
                "At least one reward type must be enabled",
                nameof(isMetricsEnabled)));

        if (isCoinsEnabled && !isCoinProductRedemptionEnabled && !isManualCoinRedemptionEnabled)
            return Result.Fail(DomainError.Validation(
                "brand.coin_redemption_types_required",
                "At least one coin redemption type must be enabled",
                nameof(isCoinProductRedemptionEnabled)));

        return Result.Ok();
    }

    private Result ValidateWelcomeRewardSettings(
        bool isWelcomeRewardsEnabled,
        IReadOnlyCollection<BrandWelcomeMetricReward> welcomeMetricRewards,
        int welcomeCoinsAmount,
        string? welcomeRewardComment)
    {
        if (welcomeCoinsAmount < 0)
            return Result.Fail(DomainError.Validation(
                "brand.welcome_coins_amount_invalid",
                "Welcome coins amount cannot be negative",
                nameof(welcomeCoinsAmount)));

        if (!IsMetricsEnabled && welcomeMetricRewards.Count > 0)
            return Result.Fail(DomainError.Validation(
                "brand.welcome_metrics_disabled",
                "Welcome metric rewards cannot be enabled when metrics are disabled",
                nameof(welcomeMetricRewards)));

        if (!IsCoinsEnabled && welcomeCoinsAmount > 0)
            return Result.Fail(DomainError.Validation(
                "brand.welcome_coins_disabled",
                "Welcome coin rewards cannot be enabled when coins are disabled",
                nameof(welcomeCoinsAmount)));

        if (isWelcomeRewardsEnabled && welcomeMetricRewards.Count == 0 && welcomeCoinsAmount == 0)
            return Result.Fail(DomainError.Validation(
                "brand.welcome_rewards_empty",
                "At least one welcome reward must be configured",
                nameof(isWelcomeRewardsEnabled)));

        if (!string.IsNullOrWhiteSpace(welcomeRewardComment) && welcomeRewardComment.Trim().Length > 200)
            return Result.Fail(DomainError.Validation(
                "brand.welcome_reward_comment_too_long",
                "Welcome reward comment cannot exceed 200 characters",
                nameof(welcomeRewardComment)));

        return Result.Ok();
    }
}
