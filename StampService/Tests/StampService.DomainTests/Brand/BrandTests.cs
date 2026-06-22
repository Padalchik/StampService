using StampService.Domain.Brand;
using BrandEntity = StampService.Domain.Brand.Brand;

namespace StampService.DomainTests.Brand;

public class BrandTests
{
    [Fact]
    public void Create_ValidData_ShouldEnableMetricsAndCoinsByDefault()
    {
        var result = BrandEntity.Create(" Coffee ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Coffee", result.Value.Name);
        Assert.True(result.Value.IsMetricsEnabled);
        Assert.True(result.Value.IsCoinsEnabled);
        Assert.True(result.Value.IsCoinProductRedemptionEnabled);
        Assert.False(result.Value.IsManualCoinRedemptionEnabled);
    }

    [Fact]
    public void UpdateDetails_ValidSettings_ShouldUpdateNameAndRewardTypes()
    {
        var brand = BrandEntity.Create("Coffee").Value;

        var result = brand.UpdateDetails(" Bakery ", isMetricsEnabled: false, isCoinsEnabled: true);

        Assert.True(result.IsSuccess);
        Assert.Equal("Bakery", brand.Name);
        Assert.False(brand.IsMetricsEnabled);
        Assert.True(brand.IsCoinsEnabled);
        Assert.True(brand.IsCoinProductRedemptionEnabled);
        Assert.False(brand.IsManualCoinRedemptionEnabled);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void UpdateDetails_WhenCoinsEnabledWithAtLeastOneRedemptionMode_ShouldUpdateSettings(
        bool isCoinProductRedemptionEnabled,
        bool isManualCoinRedemptionEnabled)
    {
        var brand = BrandEntity.Create("Coffee").Value;

        var result = brand.UpdateDetails(
            "Coffee",
            isMetricsEnabled: true,
            isCoinsEnabled: true,
            isCoinProductRedemptionEnabled,
            isManualCoinRedemptionEnabled);

        Assert.True(result.IsSuccess);
        Assert.Equal(isCoinProductRedemptionEnabled, brand.IsCoinProductRedemptionEnabled);
        Assert.Equal(isManualCoinRedemptionEnabled, brand.IsManualCoinRedemptionEnabled);
    }

    [Fact]
    public void UpdateDetails_WhenBothRewardTypesDisabled_ShouldFailWithoutChangingBrand()
    {
        var brand = BrandEntity.Create("Coffee").Value;

        var result = brand.UpdateDetails("Bakery", isMetricsEnabled: false, isCoinsEnabled: false);

        Assert.True(result.IsFailed);
        Assert.Equal("Coffee", brand.Name);
        Assert.True(brand.IsMetricsEnabled);
        Assert.True(brand.IsCoinsEnabled);
    }

    [Fact]
    public void UpdateDetails_WhenCoinsEnabledWithoutRedemptionModes_ShouldFail()
    {
        var brand = BrandEntity.Create("Coffee").Value;

        var result = brand.UpdateDetails(
            "Coffee",
            isMetricsEnabled: true,
            isCoinsEnabled: true,
            isCoinProductRedemptionEnabled: false,
            isManualCoinRedemptionEnabled: false);

        Assert.True(result.IsFailed);
        Assert.True(brand.IsCoinProductRedemptionEnabled);
        Assert.False(brand.IsManualCoinRedemptionEnabled);
    }

    [Fact]
    public void UpdateWelcomeRewardSettings_WhenRewardsAreValid_ShouldSaveSettings()
    {
        var brand = BrandEntity.Create("Coffee").Value;
        var metricId = Guid.NewGuid();

        var result = brand.UpdateWelcomeRewardSettings(
            isWelcomeRewardsEnabled: true,
            welcomeMetricRewards: [new BrandWelcomeMetricRewardSetting(metricId, 2)],
            welcomeCoinsAmount: 10,
            welcomeRewardComment: "Welcome");

        Assert.True(result.IsSuccess);
        Assert.True(brand.IsWelcomeRewardsEnabled);
        Assert.Single(brand.WelcomeMetricRewards);
        Assert.Equal(metricId, brand.WelcomeMetricRewards.Single().MetricDefinitionId);
        Assert.Equal(2, brand.WelcomeMetricRewards.Single().Amount);
        Assert.Equal(10, brand.WelcomeCoinsAmount);
        Assert.Equal("Welcome", brand.WelcomeRewardComment);
    }

    [Fact]
    public void UpdateWelcomeRewardSettings_WhenMetricsAreDisabled_ShouldRejectMetricRewards()
    {
        var brand = BrandEntity.Create("Coffee").Value;
        brand.UpdateDetails("Coffee", isMetricsEnabled: false, isCoinsEnabled: true);

        var result = brand.UpdateWelcomeRewardSettings(
            isWelcomeRewardsEnabled: true,
            welcomeMetricRewards: [new BrandWelcomeMetricRewardSetting(Guid.NewGuid(), 1)],
            welcomeCoinsAmount: 0);

        Assert.True(result.IsFailed);
        Assert.False(brand.IsWelcomeRewardsEnabled);
        Assert.Empty(brand.WelcomeMetricRewards);
    }

    [Fact]
    public void UpdateDetails_WhenFeatureIsDisabled_ShouldClearIncompatibleWelcomeRewards()
    {
        var brand = BrandEntity.Create("Coffee").Value;
        brand.UpdateWelcomeRewardSettings(
            isWelcomeRewardsEnabled: true,
            welcomeMetricRewards: [new BrandWelcomeMetricRewardSetting(Guid.NewGuid(), 1)],
            welcomeCoinsAmount: 5);

        var result = brand.UpdateDetails("Coffee", isMetricsEnabled: false, isCoinsEnabled: true);

        Assert.True(result.IsSuccess);
        Assert.False(brand.IsMetricsEnabled);
        Assert.True(brand.IsWelcomeRewardsEnabled);
        Assert.Empty(brand.WelcomeMetricRewards);
        Assert.Equal(5, brand.WelcomeCoinsAmount);
    }
}
