using StampService.Domain.Loyalty;

namespace StampService.DomainTests.Loyalty;

public class LoyaltyMetricDefinitionTests
{
    [Fact]
    public void Create_ValidData_ShouldCreateActiveMetricAndTrimValues()
    {
        var brandId = Guid.NewGuid();

        var result = LoyaltyMetricDefinition.Create(brandId, " Stamps ", 5);

        Assert.True(result.IsSuccess);
        Assert.Equal(brandId, result.Value.BrandId);
        Assert.Equal("Stamps", result.Value.Name);
        Assert.Equal(5, result.Value.RedemptionAmount);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public void Create_EmptyBrandId_ShouldFail()
    {
        var result = LoyaltyMetricDefinition.Create(Guid.Empty, "Stamps", 1);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public void Create_EmptyName_ShouldFail()
    {
        var result = LoyaltyMetricDefinition.Create(Guid.NewGuid(), " ", 1);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public void Create_NonPositiveRedemptionAmount_ShouldFail()
    {
        var result = LoyaltyMetricDefinition.Create(Guid.NewGuid(), "Stamps", 0);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldMakeMetricInactive()
    {
        var metric = CreateMetric();

        metric.Deactivate();

        Assert.False(metric.IsActive);
    }

    [Fact]
    public void Activate_WhenInactive_ShouldMakeMetricActive()
    {
        var metric = CreateMetric();
        metric.Deactivate();

        metric.Activate();

        Assert.True(metric.IsActive);
    }

    [Fact]
    public void UpdateName_ValidName_ShouldTrimAndUpdateName()
    {
        var metric = CreateMetric();

        var result = metric.UpdateName(" New name ");

        Assert.True(result.IsSuccess);
        Assert.Equal("New name", metric.Name);
    }

    [Fact]
    public void UpdateRedemptionAmount_ValidAmount_ShouldUpdateAmount()
    {
        var metric = CreateMetric();

        var result = metric.UpdateRedemptionAmount(3);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, metric.RedemptionAmount);
    }

    [Fact]
    public void UpdateDetails_WhenRedemptionAmountInvalid_ShouldFailWithoutChangingMetric()
    {
        var metric = CreateMetric();

        var result = metric.UpdateDetails("New name", 0);

        Assert.True(result.IsFailed);
        Assert.Equal("Stamps", metric.Name);
        Assert.Equal(1, metric.RedemptionAmount);
    }

    private static LoyaltyMetricDefinition CreateMetric()
    {
        return LoyaltyMetricDefinition.Create(Guid.NewGuid(), "Stamps", 1).Value;
    }
}
