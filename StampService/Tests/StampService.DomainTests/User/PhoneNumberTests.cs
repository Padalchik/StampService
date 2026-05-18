using StampService.Domain.User;

namespace StampService.DomainTests.User;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("+79991234567", "+79991234567")]
    [InlineData("+7 (999) 123-45-67", "+79991234567")]
    [InlineData("+1 212 555 0199", "+12125550199")]
    public void Normalize_WithValidInput_ShouldReturnCanonicalPhoneNumber(
        string input,
        string expected)
    {
        var result = PhoneNumber.Normalize(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("79991234567")]
    [InlineData("8 (999) 123-45-67")]
    [InlineData("+7abc9991234567")]
    [InlineData("++79991234567")]
    [InlineData("+7999")]
    [InlineData("+79991234567111")]
    [InlineData("+01234567890")]
    [InlineData("+1234567890123456")]
    public void Normalize_WithInvalidInput_ShouldFail(string input)
    {
        var result = PhoneNumber.Normalize(input);

        Assert.True(result.IsFailed);
    }
}
