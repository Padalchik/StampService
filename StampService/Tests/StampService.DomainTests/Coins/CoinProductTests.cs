using StampService.Domain.Coins;

namespace StampService.DomainTests.Coins;

public class CoinProductTests
{
    [Fact]
    public void Create_ValidData_ShouldCreateActiveProductAndTrimName()
    {
        var brandId = Guid.NewGuid();

        var result = CoinProduct.Create(brandId, " Coffee ", 10);

        Assert.True(result.IsSuccess);
        Assert.Equal(brandId, result.Value.BrandId);
        Assert.Equal("Coffee", result.Value.Name);
        Assert.Equal(10, result.Value.Price);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public void Create_EmptyBrandId_ShouldFail()
    {
        var result = CoinProduct.Create(Guid.Empty, "Coffee", 10);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public void Create_EmptyName_ShouldFail()
    {
        var result = CoinProduct.Create(Guid.NewGuid(), " ", 10);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public void Create_NonPositivePrice_ShouldFail()
    {
        var result = CoinProduct.Create(Guid.NewGuid(), "Coffee", 0);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public void UpdateDetails_ValidData_ShouldTrimAndUpdateProduct()
    {
        var product = CreateProduct();

        var result = product.UpdateDetails(" Tea ", 7);

        Assert.True(result.IsSuccess);
        Assert.Equal("Tea", product.Name);
        Assert.Equal(7, product.Price);
    }

    [Fact]
    public void UpdateDetails_WhenPriceInvalid_ShouldFailWithoutChangingProduct()
    {
        var product = CreateProduct();

        var result = product.UpdateDetails("Tea", 0);

        Assert.True(result.IsFailed);
        Assert.Equal("Coffee", product.Name);
        Assert.Equal(10, product.Price);
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldMakeProductInactive()
    {
        var product = CreateProduct();

        product.Deactivate();

        Assert.False(product.IsActive);
    }

    [Fact]
    public void Activate_WhenInactive_ShouldMakeProductActive()
    {
        var product = CreateProduct();
        product.Deactivate();

        product.Activate();

        Assert.True(product.IsActive);
    }

    private static CoinProduct CreateProduct()
    {
        return CoinProduct.Create(Guid.NewGuid(), "Coffee", 10).Value;
    }
}
