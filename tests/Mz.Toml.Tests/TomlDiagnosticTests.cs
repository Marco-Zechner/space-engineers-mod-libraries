using Xunit;

namespace Mz.Toml.Tests;

public sealed class TomlDiagnosticTests
{
    [Fact]
    public void Duplicate_Key_Reports_Second_Definition()
    {
        var result = Toml.TryParse("""
            value = 1
            value = 2

            """);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Document);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(TomlDiagnosticCode.DuplicateKey, diagnostic.Code);
        Assert.Equal(2, diagnostic.Line);
        Assert.Equal(1, diagnostic.Column);
    }

    [Fact]
    public void Missing_Value_Reports_Source_Position()
    {
        var result = Toml.TryParse("value =   # missing\n");

        Assert.False(result.IsSuccess);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(TomlDiagnosticCode.MissingValue, diagnostic.Code);
        Assert.Equal(1, diagnostic.Line);
        Assert.Equal(11, diagnostic.Column);
    }

    [Fact]
    public void Leading_Zero_Integer_Is_Rejected()
    {
        var result = Toml.TryParse("count = 01\n");

        Assert.False(result.IsSuccess);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(TomlDiagnosticCode.InvalidNumber, diagnostic.Code);
        Assert.Equal(1, diagnostic.Line);
        Assert.Equal(9, diagnostic.Column);
    }

    [Fact]
    public void Decimal_Float_Overflow_Is_Rejected()
    {
        var result = Toml.TryParse("value = 1e9999\n");

        Assert.False(result.IsSuccess);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(TomlDiagnosticCode.InvalidNumber, diagnostic.Code);
        Assert.Equal(1, diagnostic.Line);
        Assert.Equal(9, diagnostic.Column);
    }

    [Fact]
    public void Lone_Carriage_Return_Is_Rejected()
    {
        // not a raw string literal because we want to test the CR character
        var result = Toml.TryParse(
            "a = 1\r" +
            "b = 2\n");

        Assert.False(result.IsSuccess);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(TomlDiagnosticCode.InvalidNewline, diagnostic.Code);
        Assert.Equal(1, diagnostic.Line);
        Assert.Equal(6, diagnostic.Column);
    }

    [Fact]
    public void Unterminated_String_Is_Rejected()
    {
        var result = Toml.TryParse("name = \"unfinished\n");

        Assert.False(result.IsSuccess);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(TomlDiagnosticCode.InvalidString, diagnostic.Code);
        Assert.Equal(1, diagnostic.Line);
        Assert.Equal(8, diagnostic.Column);
    }

    [Fact]
    public void Control_Character_In_Full_Line_Comment_Is_Rejected()
    {
        var result = Toml.TryParse("# invalid" + '\u0001' + "\n");

        Assert.False(result.IsSuccess);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(TomlDiagnosticCode.InvalidComment, diagnostic.Code);
        Assert.Equal(1, diagnostic.Line);
        Assert.Equal(10, diagnostic.Column);
    }

    [Fact]
    public void Control_Character_In_Trailing_Comment_Is_Rejected()
    {
        var result = Toml.TryParse("value = 1 # bad" + '\u007F' + "\n");

        Assert.False(result.IsSuccess);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(TomlDiagnosticCode.InvalidComment, diagnostic.Code);
        Assert.Equal(1, diagnostic.Line);
        Assert.Equal(16, diagnostic.Column);
    }

    [Fact]
    public void Tab_In_Comment_Is_Allowed()
    {
        // not a raw string literal because we want to test the tab character
        var document = Toml.Parse(
            "# comment\ttext\n" +
            "value = 1 # trailing\tcomment\n");

        Assert.Equal(1L, document.Root.AsValue("value").AsInteger());
    }

    [Fact]
    public void Throwing_Parse_Exposes_Diagnostic()
    {
        var exception = Assert.Throws<TomlParseException>(() => Toml.Parse("broken\n"));

        Assert.NotNull(exception.Diagnostic);
        Assert.Equal(TomlDiagnosticCode.MissingEquals, exception.Diagnostic.Code);
    }
}
