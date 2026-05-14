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
}
