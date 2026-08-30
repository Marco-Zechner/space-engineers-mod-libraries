using Xunit;

namespace Mz.Toml.Tests;

public sealed class TomlNumericParsingTests
{
    [Fact]
    public void Parses_Signed_Decimal_Integer_Range()
    {
        var document = Toml.Parse(
            """
            max = 9223372036854775807
            min = -9223372036854775808
            plus = +42
            minus = -42

            """);

        Assert.Equal(long.MaxValue, document.Root.AsValue("max").AsInteger());

        Assert.Equal(long.MinValue, document.Root.AsValue("min").AsInteger());

        Assert.Equal(42L, document.Root.AsValue("plus").AsInteger());

        Assert.Equal(-42L, document.Root.AsValue("minus").AsInteger());
    }

    [Fact]
    public void Parses_Integer_Bases_And_Underscores()
    {
        var document = Toml.Parse("""
            hex = 0xdead_beef
            oct = 0o7_6_5
            bin = 0b1_0_1
            decimal = 9_007_199_254_740_991

            """);

        Assert.Equal(3735928559L, document.Root.AsValue("hex").AsInteger());

        Assert.Equal(501L, document.Root.AsValue("oct").AsInteger());

        Assert.Equal(5L, document.Root.AsValue("bin").AsInteger());

        Assert.Equal(9007199254740991L, document.Root.AsValue("decimal").AsInteger());
    }

    [Fact]
    public void Base_Integer_Leading_Zeroes_Are_Valid()
    {
        var document = Toml.Parse("""
            hex = 0x00987
            oct = 0o000755
            bin = 0b000101

            """);

        Assert.Equal(2439L, document.Root.AsValue("hex").AsInteger());

        Assert.Equal(493L, document.Root.AsValue("oct").AsInteger());

        Assert.Equal(5L, document.Root.AsValue("bin").AsInteger());
    }

    [Theory]
    [InlineData("value = 01\n")]
    [InlineData("value = -01\n")]
    [InlineData("value = +01\n")]
    [InlineData("value = 0_0\n")]
    [InlineData("value = +0_1\n")]
    public void Decimal_Integer_Leading_Zeroes_Are_Rejected(string text) 
        => Assert.False(Toml.TryParse(text).IsSuccess);

    [Theory]
    [InlineData("value = +0xff\n")]
    [InlineData("value = -0xff\n")]
    [InlineData("value = +0o755\n")]
    [InlineData("value = -0o755\n")]
    [InlineData("value = +0b1\n")]
    [InlineData("value = -0b1\n")]
    public void Base_Integers_Cannot_Have_A_Sign(string text) 
        => Assert.False(Toml.TryParse(text).IsSuccess);

    [Theory]
    [InlineData("value = 0X1\n")]
    [InlineData("value = 0O1\n")]
    [InlineData("value = 0B1\n")]
    [InlineData("value = 0x_1\n")]
    [InlineData("value = 0x1_\n")]
    [InlineData("value = 0x1__2\n")]
    [InlineData("value = 0o778\n")]
    [InlineData("value = 0b0012\n")]
    public void Invalid_Base_Integer_Syntax_Is_Rejected(string text) 
        => Assert.False(Toml.TryParse(text).IsSuccess);

    [Theory]
    [InlineData("value = 9223372036854775808\n")]
    [InlineData("value = -9223372036854775809\n")]
    [InlineData("value = 0x8000000000000000\n")]
    public void Integer_Overflow_Is_Rejected(string text) 
        => Assert.False(Toml.TryParse(text).IsSuccess);

    [Fact]
    public void Parses_Fractions_And_Exponents()
    {
        var document = Toml.Parse("""
            fraction = +3.1415
            lower = 3e-2
            upper = 3E+2
            mixed = -3.1e2
            underscores = 3_141.592_7
            expUnderscore = 3e1_4

            """);

        Assert.Equal(3.1415, document.Root.AsValue("fraction").AsFloat());

        Assert.Equal(0.03, document.Root.AsValue("lower").AsFloat());

        Assert.Equal(300.0, document.Root.AsValue("upper").AsFloat());

        Assert.Equal(-310.0, document.Root.AsValue("mixed").AsFloat());

        Assert.Equal(3141.5927, document.Root.AsValue("underscores").AsFloat());

        Assert.Equal(3.0e14, document.Root.AsValue("expUnderscore").AsFloat());
    }

    [Theory]
    [InlineData("value = .1\n")]
    [InlineData("value = -.1\n")]
    [InlineData("value = 1.\n")]
    [InlineData("value = 1.e2\n")]
    [InlineData("value = 1_.2\n")]
    [InlineData("value = 1._2\n")]
    [InlineData("value = 1.2_e2\n")]
    [InlineData("value = 1e_2\n")]
    [InlineData("value = 1e2_\n")]
    [InlineData("value = 1e__2\n")]
    [InlineData("value = 03.14\n")]
    [InlineData("value = -03.14\n")]
    public void Invalid_Float_Syntax_Is_Rejected(string text) 
        => Assert.False(Toml.TryParse(text).IsSuccess);

    [Fact]
    public void Parses_Special_Floats()
    {
        var document = Toml.Parse("""
            inf = inf
            plusInf = +inf
            minusInf = -inf
            nan = nan
            plusNan = +nan
            minusNan = -nan

            """);

        Assert.True(double.IsPositiveInfinity(document.Root.AsValue("inf").AsFloat()));

        Assert.True(double.IsPositiveInfinity(document.Root.AsValue("plusInf").AsFloat()));

        Assert.True(double.IsNegativeInfinity(document.Root.AsValue("minusInf").AsFloat()));

        Assert.True(double.IsNaN(document.Root.AsValue("nan").AsFloat()));

        Assert.True(double.IsNaN(document.Root.AsValue("plusNan").AsFloat()));

        Assert.True(double.IsNaN(document.Root.AsValue("minusNan").AsFloat()));
    }

    [Theory]
    [InlineData("value = Inf\n")]
    [InlineData("value = NaN\n")]
    [InlineData("value = in_f\n")]
    [InlineData("value = na_n\n")]
    [InlineData("value = in\n")]
    [InlineData("value = na\n")]
    public void Invalid_Special_Floats_Are_Rejected(string text) 
        => Assert.False(Toml.TryParse(text).IsSuccess);

    [Fact]
    public void Negative_Floating_Zero_Is_Preserved()
    {
        var document = Toml.Parse("""
            fraction = -0.0
            exponent = -0e0

            """);

        AssertNegativeZero(document.Root.AsValue("fraction").AsFloat());

        AssertNegativeZero(document.Root.AsValue("exponent").AsFloat());
    }

    [Fact]
    public void Writer_Preserves_Negative_Floating_Zero()
    {
        var document = Toml.Parse("value = -0.0\n");

        var text = Toml.Write(document);

        Assert.Equal("value = -0.0\n", text);

        AssertNegativeZero(Toml.Parse(text).Root.AsValue("value").AsFloat());
    }

    [Fact]
    public void Writer_Canonicalizes_Base_And_Underscored_Integers()
    {
        var document = Toml.Parse("""
            hex = 0xdead_beef
            decimal = 1_000

            """);

        Assert.Equal("""
            hex = 3735928559
            decimal = 1000

            """, 
            Toml.Write(document));
    }

    [Fact]
    public void Writer_Canonicalizes_Special_Floats()
    {
        var document = Toml.Parse("""
            a = +inf
            b = -inf
            c = -nan

            """);

        Assert.Equal("""
            a = inf
            b = -inf
            c = nan

            """,
            Toml.Write(document));
    }

    private static void AssertNegativeZero(double value)
    {
        Assert.Equal(0.0, value);
        Assert.True(double.IsNegativeInfinity(1.0 / value));
    }
}