using MiniPdm.Domain.Validation;

namespace MiniPdm.Domain.Tests.Validation;

public class DesignationValidatorTests
{
    [Theory]
    [InlineData("АБВГ.301245.001")]
    [InlineData("РДЦЛ.304112.302")]
    [InlineData("ЁЖЗИ.000001.999")]
    public void IsValid_WithValidDesignation_ReturnsTrue(string designation)
    {
        var result = DesignationValidator.IsValid(designation);

        Assert.True(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("PДЦЛ.304112.601")]
    [InlineData("РДЦЛ.30411.602")]
    [InlineData("РДЦЛ.304112.60")]
    [InlineData("РДЦЛ304112.602")]
    [InlineData("рдцл.304112.602")]
    public void IsValid_WithInvalidDesignation_ReturnsFalse(string? designation)
    {
        var result = DesignationValidator.IsValid(designation);

        Assert.False(result);
    }
}