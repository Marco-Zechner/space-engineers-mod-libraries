using System;
using Mz.Toml;
using Xunit;

namespace Mz.Toml.Tests
{
    public sealed class TomlNumericParsingTests
    {
        [Fact]
        public void Parses_Signed_Decimal_Integer_Range()
        {
            var document = Toml.Parse(
                "max = 9223372036854775807\n" +
                "min = -9223372036854775808\n" +
                "plus = +42\n" +
                "minus = -42\n");

            Assert.Equal(
                long.MaxValue,
                Value(document.Root, "max").AsInteger());

            Assert.Equal(
                long.MinValue,
                Value(document.Root, "min").AsInteger());

            Assert.Equal(
                42L,
                Value(document.Root, "plus").AsInteger());

            Assert.Equal(
                -42L,
                Value(document.Root, "minus").AsInteger());
        }

        [Fact]
        public void Parses_Integer_Bases_And_Underscores()
        {
            var document = Toml.Parse(
                "hex = 0xdead_beef\n" +
                "oct = 0o7_6_5\n" +
                "bin = 0b1_0_1\n" +
                "decimal = 9_007_199_254_740_991\n");

            Assert.Equal(
                3735928559L,
                Value(document.Root, "hex").AsInteger());

            Assert.Equal(
                501L,
                Value(document.Root, "oct").AsInteger());

            Assert.Equal(
                5L,
                Value(document.Root, "bin").AsInteger());

            Assert.Equal(
                9007199254740991L,
                Value(document.Root, "decimal").AsInteger());
        }

        [Fact]
        public void Base_Integer_Leading_Zeroes_Are_Valid()
        {
            var document = Toml.Parse(
                "hex = 0x00987\n" +
                "oct = 0o000755\n" +
                "bin = 0b000101\n");

            Assert.Equal(
                2439L,
                Value(document.Root, "hex").AsInteger());

            Assert.Equal(
                493L,
                Value(document.Root, "oct").AsInteger());

            Assert.Equal(
                5L,
                Value(document.Root, "bin").AsInteger());
        }

        [Theory]
        [InlineData("value = 01\n")]
        [InlineData("value = -01\n")]
        [InlineData("value = +01\n")]
        [InlineData("value = 0_0\n")]
        [InlineData("value = +0_1\n")]
        public void Decimal_Integer_Leading_Zeroes_Are_Rejected(
            string text)
        {
            Assert.False(
                Toml.TryParse(text).IsSuccess);
        }

        [Theory]
        [InlineData("value = +0xff\n")]
        [InlineData("value = -0xff\n")]
        [InlineData("value = +0o755\n")]
        [InlineData("value = -0o755\n")]
        [InlineData("value = +0b1\n")]
        [InlineData("value = -0b1\n")]
        public void Base_Integers_Cannot_Have_A_Sign(
            string text)
        {
            Assert.False(
                Toml.TryParse(text).IsSuccess);
        }

        [Theory]
        [InlineData("value = 0X1\n")]
        [InlineData("value = 0O1\n")]
        [InlineData("value = 0B1\n")]
        [InlineData("value = 0x_1\n")]
        [InlineData("value = 0x1_\n")]
        [InlineData("value = 0x1__2\n")]
        [InlineData("value = 0o778\n")]
        [InlineData("value = 0b0012\n")]
        public void Invalid_Base_Integer_Syntax_Is_Rejected(
            string text)
        {
            Assert.False(
                Toml.TryParse(text).IsSuccess);
        }

        [Theory]
        [InlineData("value = 9223372036854775808\n")]
        [InlineData("value = -9223372036854775809\n")]
        [InlineData("value = 0x8000000000000000\n")]
        public void Integer_Overflow_Is_Rejected(
            string text)
        {
            Assert.False(
                Toml.TryParse(text).IsSuccess);
        }

        [Fact]
        public void Parses_Fractions_And_Exponents()
        {
            var document = Toml.Parse(
                "fraction = +3.1415\n" +
                "lower = 3e-2\n" +
                "upper = 3E+2\n" +
                "mixed = -3.1e2\n" +
                "underscores = 3_141.592_7\n" +
                "expUnderscore = 3e1_4\n");

            Assert.Equal(
                3.1415,
                Value(document.Root, "fraction").AsFloat());

            Assert.Equal(
                0.03,
                Value(document.Root, "lower").AsFloat());

            Assert.Equal(
                300.0,
                Value(document.Root, "upper").AsFloat());

            Assert.Equal(
                -310.0,
                Value(document.Root, "mixed").AsFloat());

            Assert.Equal(
                3141.5927,
                Value(document.Root, "underscores").AsFloat());

            Assert.Equal(
                3.0e14,
                Value(document.Root, "expUnderscore").AsFloat());
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
        public void Invalid_Float_Syntax_Is_Rejected(
            string text)
        {
            Assert.False(
                Toml.TryParse(text).IsSuccess);
        }

        [Fact]
        public void Parses_Special_Floats()
        {
            var document = Toml.Parse(
                "inf = inf\n" +
                "plusInf = +inf\n" +
                "minusInf = -inf\n" +
                "nan = nan\n" +
                "plusNan = +nan\n" +
                "minusNan = -nan\n");

            Assert.True(
                double.IsPositiveInfinity(
                    Value(document.Root, "inf").AsFloat()));

            Assert.True(
                double.IsPositiveInfinity(
                    Value(document.Root, "plusInf").AsFloat()));

            Assert.True(
                double.IsNegativeInfinity(
                    Value(document.Root, "minusInf").AsFloat()));

            Assert.True(
                double.IsNaN(
                    Value(document.Root, "nan").AsFloat()));

            Assert.True(
                double.IsNaN(
                    Value(document.Root, "plusNan").AsFloat()));

            Assert.True(
                double.IsNaN(
                    Value(document.Root, "minusNan").AsFloat()));
        }

        [Theory]
        [InlineData("value = Inf\n")]
        [InlineData("value = NaN\n")]
        [InlineData("value = in_f\n")]
        [InlineData("value = na_n\n")]
        [InlineData("value = in\n")]
        [InlineData("value = na\n")]
        public void Invalid_Special_Floats_Are_Rejected(
            string text)
        {
            Assert.False(
                Toml.TryParse(text).IsSuccess);
        }

        [Fact]
        public void Negative_Floating_Zero_Is_Preserved()
        {
            var document = Toml.Parse(
                "fraction = -0.0\n" +
                "exponent = -0e0\n");

            AssertNegativeZero(
                Value(document.Root, "fraction").AsFloat());

            AssertNegativeZero(
                Value(document.Root, "exponent").AsFloat());
        }

        [Fact]
        public void Writer_Preserves_Negative_Floating_Zero()
        {
            var document = Toml.Parse(
                "value = -0.0\n");

            var text = Toml.Write(document);

            Assert.Equal(
                "value = -0.0\n",
                text);

            AssertNegativeZero(
                Value(
                    Toml.Parse(text).Root,
                    "value").AsFloat());
        }

        [Fact]
        public void Writer_Canonicalizes_Base_And_Underscored_Integers()
        {
            var document = Toml.Parse(
                "hex = 0xdead_beef\n" +
                "decimal = 1_000\n");

            Assert.Equal(
                "hex = 3735928559\n" +
                "decimal = 1000\n",
                Toml.Write(document));
        }

        [Fact]
        public void Writer_Canonicalizes_Special_Floats()
        {
            var document = Toml.Parse(
                "a = +inf\n" +
                "b = -inf\n" +
                "c = -nan\n");

            Assert.Equal(
                "a = inf\n" +
                "b = -inf\n" +
                "c = nan\n",
                Toml.Write(document));
        }

        private static void AssertNegativeZero(
            double value)
        {
            Assert.Equal(
                0.0,
                value);

            Assert.True(
                double.IsNegativeInfinity(
                    1.0 / value));
        }

        private static TomlValue Value(
            TomlTable table,
            string key)
        {
            return Assert.IsType<TomlValue>(
                table[key]);
        }
    }
}