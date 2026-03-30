using InteractiveLeads.Application.Common.PhoneNumbers;

namespace InteractiveLeads.Tests;

public sealed class PhoneNumberNormalizerTests
{
    [Theory]
    [InlineData("11987654321", "5511987654321")]
    [InlineData("(11) 98765-4321", "5511987654321")]
    [InlineData("+55 11 98765-4321", "5511987654321")]
    [InlineData("5511987654321", "5511987654321")]
    public void Brazil_mobile_national_formats_normalize_to_e164_digits(string input, string expected)
    {
        Assert.Equal(expected, PhoneNumberNormalizer.ToNormalizedDigits(input, "BR"));
    }

    [Theory]
    [InlineData("1187654321", "5511987654321")]
    [InlineData("+55 11 8765-4321", "5511987654321")]
    [InlineData("551187654321", "5511987654321")]
    public void Brazil_legacy_mobile_missing_nine_gets_prefix(string input, string expected)
    {
        Assert.Equal(expected, PhoneNumberNormalizer.ToNormalizedDigits(input, "BR"));
    }

    [Theory]
    [InlineData("+1 202 555 0123", "US", "12025550123")]
    public void Non_brazil_respects_default_region(string input, string region, string expected)
    {
        Assert.Equal(expected, PhoneNumberNormalizer.ToNormalizedDigits(input, region));
    }

    [Fact]
    public void Empty_returns_empty()
    {
        Assert.Equal(string.Empty, PhoneNumberNormalizer.ToNormalizedDigits(null, "BR"));
        Assert.Equal(string.Empty, PhoneNumberNormalizer.ToNormalizedDigits("   ", "BR"));
    }
}
