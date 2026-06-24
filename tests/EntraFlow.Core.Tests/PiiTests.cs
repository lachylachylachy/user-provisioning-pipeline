using EntraFlow.Core.Logging;

namespace EntraFlow.Core.Tests;

public class PiiTests
{
    [Theory]
    [InlineData("jane.doe@company.com", "ja******@company.com")]
    [InlineData("ab@x.com", "a*@x.com")]
    [InlineData("a@x.com", "a*@x.com")]
    public void Mask_KeepsShortPrefixAndDomain(string input, string expected)
    {
        Assert.Equal(expected, Pii.Mask(input));
    }

    [Fact]
    public void Mask_NonEmail_KeepsFirstCharOnly()
    {
        Assert.Equal("S******", Pii.Mask("Secret1"));
    }

    [Fact]
    public void Mask_Empty_ReturnsEmpty()
    {
        Assert.Equal("", Pii.Mask(""));
        Assert.Equal("", Pii.Mask(null));
    }
}
