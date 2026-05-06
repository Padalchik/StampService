using StampService.Domain.Loyalty;

namespace StampService.DomainTests.Loyalty;

public class MetricBalanceTests
{
    [Fact]
    public void Create_ValidData_ShouldCreateZeroBalance()
    {
        var userId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var metricDefinitionId = Guid.NewGuid();

        var result = MetricBalance.Create(userId, brandId, metricDefinitionId);

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value.UserId);
        Assert.Equal(brandId, result.Value.BrandId);
        Assert.Equal(metricDefinitionId, result.Value.MetricDefinitionId);
        Assert.Equal(0, result.Value.Value);
    }

    [Fact]
    public void Add_PositiveAmount_ShouldIncreaseValue()
    {
        var balance = CreateBalance();

        var result = balance.Add(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, balance.Value);
    }

    [Fact]
    public void Add_NonPositiveAmount_ShouldFail()
    {
        var balance = CreateBalance();

        var result = balance.Add(0);

        Assert.True(result.IsFailed);
        Assert.Equal(0, balance.Value);
    }

    [Fact]
    public void Subtract_WhenEnoughValue_ShouldDecreaseValue()
    {
        var balance = CreateBalance();
        balance.Add(5);

        var result = balance.Subtract(3);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, balance.Value);
    }

    [Fact]
    public void Subtract_WhenInsufficientValue_ShouldFailAndKeepValue()
    {
        var balance = CreateBalance();
        balance.Add(2);

        var result = balance.Subtract(3);

        Assert.True(result.IsFailed);
        Assert.Equal(2, balance.Value);
    }

    [Fact]
    public void SetMaterializedValue_NonNegativeValue_ShouldSetValue()
    {
        var balance = CreateBalance();
        balance.Add(2);

        var result = balance.SetMaterializedValue(10);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, balance.Value);
    }

    [Fact]
    public void SetMaterializedValue_NegativeValue_ShouldFailAndKeepValue()
    {
        var balance = CreateBalance();
        balance.Add(2);

        var result = balance.SetMaterializedValue(-1);

        Assert.True(result.IsFailed);
        Assert.Equal(2, balance.Value);
    }

    private static MetricBalance CreateBalance()
    {
        return MetricBalance.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Value;
    }
}
