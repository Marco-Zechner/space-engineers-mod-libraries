using Xunit;

namespace Mz.Toml.Tests;

public sealed class TomlScalarParsingTests
{
    [Fact]
    public void Parses_Root_Scalars_Whitespace_And_Comments()
    {
        const string text = """
            # application settings
            enabled = true # trailing comment
            count = 42
            ratio = -1.25e2
            name = "hello # still string"


            """;

        var result = Toml.TryParse(text);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Diagnostics);

        var root = result.Document!.Root;
        Assert.Equal(4, root.Count);

        var enabled = root.AsValue("enabled");
        var count = root.AsValue("count");
        var ratio = root.AsValue("ratio");
        var name = root.AsValue("name");

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
        const string text = "value = \"line\\nquote=\\\" slash=\\\\ unicode=\\u263A\"\n";

        var document = Toml.Parse(text);
        var value = document.Root.AsValue("value");

        Assert.Equal("line\nquote=\" slash=\\ unicode=\u263A", value.AsString());
    }

    [Fact]
    public void Accepts_Lf_And_Crlf_Newlines()
    {
        var lf = Toml.Parse("""
            first = 1
            second = 2

            """);

        // not a raw string literal because we want to test the CRLF characters
        var crlf = Toml.Parse(
            "first = 1\r\n" +
            "second = 2\r\n");

        var first = crlf.Root.AsValue("first");
        Assert.Equal(lf.Root.AsValue("first").AsInteger(), first.AsInteger());

        var second = crlf.Root.AsValue("second");
        Assert.Equal(2L, second.AsInteger());
        Assert.Equal(2, second.Line);
        Assert.Equal(10, second.Column);
    }

    [Fact]
    public void Parses_Toml_Special_Floats()
    {
        var document = Toml.Parse("""
            positive = inf
            positiveSigned = +inf
            negative = -inf
            nan = nan
            nanPositive = +nan
            nanNegative = -nan

            """);

        Assert.True(double.IsPositiveInfinity(document.Root.AsValue("positive").AsFloat()));

        Assert.True(double.IsPositiveInfinity(document.Root.AsValue("positiveSigned").AsFloat()));

        Assert.True(double.IsNegativeInfinity(document.Root.AsValue("negative").AsFloat()));

        Assert.True(double.IsNaN(document.Root.AsValue("nan").AsFloat()));

        Assert.True(double.IsNaN(document.Root.AsValue("nanPositive").AsFloat()));

        Assert.True(double.IsNaN(document.Root.AsValue("nanNegative").AsFloat()));
    }

    [Fact]
    public void Writer_Roundtrip_Preserves_Slice_One_Semantics()
    {
        const string text = """
            name = "hello"
            count = -12
            ratio = 1.5
            enabled = false

            """;

        var first = Toml.Parse(text);
        var written = Toml.Write(first);
        var second = Toml.Parse(written);

        Assert.Equal("""
            name = "hello"
            count = -12
            ratio = 1.5
            enabled = false

            """.ReplaceLineEndings("\n"),
            written);

        Assert.Equal("hello", second.Root.AsValue("name").AsString());

        Assert.Equal(-12L, second.Root.AsValue("count").AsInteger());

        Assert.Equal(1.5, second.Root.AsValue("ratio").AsFloat());

        Assert.False(second.Root.AsValue("enabled").AsBoolean());
    }

    [Fact]
    public void Programmatic_Document_Writes_Deterministically()
    {
        var document = new TomlDocument();
        document.Root.Set("name", TomlValue.FromString("demo"));
        document.Root.Set("count", TomlValue.FromInteger(7));
        document.Root.Set("ratio", TomlValue.FromFloat(2));
        document.Root.Set("enabled", TomlValue.FromBoolean(true));

        Assert.Equal("""
            name = "demo"
            count = 7
            ratio = 2.0
            enabled = true

            """.ReplaceLineEndings("\n"),
            Toml.Write(document));
    }

    [Fact]
    public void Programmatic_Special_Floats_Write_And_Parse()
    {
        var document = new TomlDocument();
        document.Root.Set("positive", TomlValue.FromFloat(double.PositiveInfinity));
        document.Root.Set("negative", TomlValue.FromFloat(double.NegativeInfinity));
        document.Root.Set("notNumber", TomlValue.FromFloat(double.NaN));

        var text = Toml.Write(document);

        Assert.Equal("""
            positive = inf
            negative = -inf
            notNumber = nan

            """.ReplaceLineEndings("\n"),
            text);

        var restored = Toml.Parse(text);

        Assert.True(double.IsPositiveInfinity(restored.Root.AsValue("positive").AsFloat()));

        Assert.True(double.IsNegativeInfinity(restored.Root.AsValue("negative").AsFloat()));

        Assert.True(double.IsNaN(restored.Root.AsValue("notNumber").AsFloat()));
    }
}
