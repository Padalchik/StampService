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
}
