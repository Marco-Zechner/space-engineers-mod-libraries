using System;
using Mz.Toml;
using Xunit;

namespace Mz.Toml.Tests
{
    public sealed class TomlScalarParsingTests
    {
        [Fact]
        public void Parses_Root_Scalars_Whitespace_And_Comments()
        {
            const string text =
                "# application settings\n" +
                "enabled = true # trailing comment\n" +
                "count = 42\n" +
                "ratio = -1.25e2\n" +
                "name = \"hello # still string\"\n" +
                "\n";

            var result = Toml.TryParse(text);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Diagnostics);

            var root = result.Document!.Root;
            Assert.Equal(4, root.Count);

            var enabled = Assert.IsType<TomlValue>(root["enabled"]);
            var count = Assert.IsType<TomlValue>(root["count"]);
            var ratio = Assert.IsType<TomlValue>(root["ratio"]);
            var name = Assert.IsType<TomlValue>(root["name"]);

            Assert.Equal(TomlValueKind.Boolean, enabled.ValueKind);
            Assert.True(enabled.AsBoolean());

            Assert.Equal(TomlValueKind.Integer, count.ValueKind);
            Assert.Equal(42L, count.AsInteger());

            Assert.Equal(TomlValueKind.Float, ratio.ValueKind);
            Assert.Equal(-125.0, ratio.AsFloat());

            Assert.Equal(TomlValueKind.String, name.ValueKind);
            Assert.Equal("hello # still string", name.AsString());

            Assert.Equal(2, enabled.Line);
            Assert.Equal(11, enabled.Column);
        }

        [Fact]
        public void Parses_Basic_String_Escapes_And_Unicode()
        {
            const string text =
                "value = \"line\\nquote=\\\" slash=\\\\ unicode=\\u263A\"\n";

            var document = Toml.Parse(text);
            var value = Assert.IsType<TomlValue>(document.Root["value"]);

            Assert.Equal(
                "line\nquote=\" slash=\\ unicode=\u263A",
                value.AsString());
        }

        [Fact]
        public void Accepts_Lf_And_Crlf_Newlines()
        {
            var lf = Toml.Parse(
                "first = 1\n" +
                "second = 2\n");

            var crlf = Toml.Parse(
                "first = 1\r\n" +
                "second = 2\r\n");

            Assert.Equal(
                Assert.IsType<TomlValue>(lf.Root["first"]).AsInteger(),
                Assert.IsType<TomlValue>(crlf.Root["first"]).AsInteger());

            var second = Assert.IsType<TomlValue>(crlf.Root["second"]);
            Assert.Equal(2L, second.AsInteger());
            Assert.Equal(2, second.Line);
            Assert.Equal(10, second.Column);
        }

        [Fact]
        public void Parses_Toml_Special_Floats()
        {
            var document = Toml.Parse(
                "positive = inf\n" +
                "positiveSigned = +inf\n" +
                "negative = -inf\n" +
                "nan = nan\n" +
                "nanPositive = +nan\n" +
                "nanNegative = -nan\n");

            Assert.True(
                double.IsPositiveInfinity(
                    Assert.IsType<TomlValue>(
                        document.Root["positive"]).AsFloat()));

            Assert.True(
                double.IsPositiveInfinity(
                    Assert.IsType<TomlValue>(
                        document.Root["positiveSigned"]).AsFloat()));

            Assert.True(
                double.IsNegativeInfinity(
                    Assert.IsType<TomlValue>(
                        document.Root["negative"]).AsFloat()));

            Assert.True(
                double.IsNaN(
                    Assert.IsType<TomlValue>(
                        document.Root["nan"]).AsFloat()));

            Assert.True(
                double.IsNaN(
                    Assert.IsType<TomlValue>(
                        document.Root["nanPositive"]).AsFloat()));

            Assert.True(
                double.IsNaN(
                    Assert.IsType<TomlValue>(
                        document.Root["nanNegative"]).AsFloat()));
        }

        [Fact]
        public void Writer_Roundtrip_Preserves_Slice_One_Semantics()
        {
            const string text =
                "name = \"hello\"\n" +
                "count = -12\n" +
                "ratio = 1.5\n" +
                "enabled = false\n";

            var first = Toml.Parse(text);
            var written = Toml.Write(first);
            var second = Toml.Parse(written);

            Assert.Equal(
                "name = \"hello\"\n" +
                "count = -12\n" +
                "ratio = 1.5\n" +
                "enabled = false\n",
                written);

            Assert.Equal(
                "hello",
                Assert.IsType<TomlValue>(
                    second.Root["name"]).AsString());

            Assert.Equal(
                -12L,
                Assert.IsType<TomlValue>(
                    second.Root["count"]).AsInteger());

            Assert.Equal(
                1.5,
                Assert.IsType<TomlValue>(
                    second.Root["ratio"]).AsFloat());

            Assert.False(
                Assert.IsType<TomlValue>(
                    second.Root["enabled"]).AsBoolean());
        }

        [Fact]
        public void Programmatic_Document_Writes_Deterministically()
        {
            var document = new TomlDocument();
            document.Root.Set("name", TomlValue.FromString("demo"));
            document.Root.Set("count", TomlValue.FromInteger(7));
            document.Root.Set("ratio", TomlValue.FromFloat(2));
            document.Root.Set("enabled", TomlValue.FromBoolean(true));

            Assert.Equal(
                "name = \"demo\"\n" +
                "count = 7\n" +
                "ratio = 2.0\n" +
                "enabled = true\n",
                Toml.Write(document));
        }

        [Fact]
        public void Programmatic_Special_Floats_Write_And_Parse()
        {
            var document = new TomlDocument();
            document.Root.Set(
                "positive",
                TomlValue.FromFloat(double.PositiveInfinity));
            document.Root.Set(
                "negative",
                TomlValue.FromFloat(double.NegativeInfinity));
            document.Root.Set(
                "notNumber",
                TomlValue.FromFloat(double.NaN));

            var text = Toml.Write(document);

            Assert.Equal(
                "positive = inf\n" +
                "negative = -inf\n" +
                "notNumber = nan\n",
                text);

            var restored = Toml.Parse(text);

            Assert.True(
                double.IsPositiveInfinity(
                    Assert.IsType<TomlValue>(
                        restored.Root["positive"]).AsFloat()));

            Assert.True(
                double.IsNegativeInfinity(
                    Assert.IsType<TomlValue>(
                        restored.Root["negative"]).AsFloat()));

            Assert.True(
                double.IsNaN(
                    Assert.IsType<TomlValue>(
                        restored.Root["notNumber"]).AsFloat()));
        }
    }
}