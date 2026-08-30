using Xunit;

namespace Mz.Toml.Tests;

public sealed class TomlTableDiagnosticTests
{
    [Theory]
    [InlineData("a = false\na.b = true\n")]
    [InlineData("\"\" = 1\n''.x = 2\n")]
    [InlineData("a.b = 1\na.b.c = 2\n")]
    [InlineData("[a]\nb = 1\n[a.b]\nc = 2\n")]
    [InlineData("a = 1\n[a.b.c.d]\n")]
    public void Scalar_Cannot_Be_Traversed_As_Table(string text)
    {
        var result = Toml.TryParse(text);

        Assert.False(result.IsSuccess);
        Assert.Equal(TomlDiagnosticCode.TableConflict, Assert.Single(result.Diagnostics).Code);
    }

    [Theory]
    [InlineData("""
        [a.b.c]
        z = 9
        [a]
        b.c.t = "invalid"

        """)]
    [InlineData("""
        [a.b.c.d]
        z = 9
        [a]
        b.c.d.k.t = "invalid"

        """)]
    public void Dotted_Key_Cannot_Append_Through_Explicit_Table(string text)
    {
        var result = Toml.TryParse(text);

        Assert.False(result.IsSuccess);
        Assert.Equal(TomlDiagnosticCode.TableConflict, Assert.Single(result.Diagnostics).Code);
    }
    [Fact]
    public void Dotted_Key_Table_Cannot_Later_Be_Explicitly_Defined()
    {
        var result = Toml.TryParse("""
            [t1]
            t2.t3.v = 0
            [t1.t2]

            """);

        Assert.False(result.IsSuccess);
        Assert.Equal(TomlDiagnosticCode.TableConflict, Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void Deep_Dotted_Key_Table_Cannot_Later_Be_Explicitly_Defined()
    {
        var result = Toml.TryParse("""
            [t1]
            t2.t3.v = 0
            [t1.t2.t3]

            """);

        Assert.False(result.IsSuccess);
        Assert.Equal(TomlDiagnosticCode.TableConflict, Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void Explicit_Table_Cannot_Be_Defined_Twice()
    {
        var result = Toml.TryParse("""
            [a.b]
            [a]
            [a]

            """);

        Assert.False(result.IsSuccess);
        Assert.Equal(TomlDiagnosticCode.DuplicateTable, Assert.Single(result.Diagnostics).Code);
    }

    [Theory]
    [InlineData("""
        name = "Tom"
        name = "Pradyun"

        """)]
    [InlineData("""
        spelling = "favorite"
        "spelling" = "favourite"

        """)]
    [InlineData("""
        spelling = "favorite"
        'spelling' = "favourite"

        """)]
    [InlineData("""
        "\u0061" = 1
        a = 2

        """)]
    public void Equivalent_Key_Definitions_Are_Duplicates(string text)
    {
        var result = Toml.TryParse(text);

        Assert.False(result.IsSuccess);
        Assert.Equal(TomlDiagnosticCode.DuplicateKey, Assert.Single(result.Diagnostics).Code);
    }

    [Theory]
    [InlineData(". = 1\n")]
    [InlineData(".. = 1\n")]
    [InlineData(".key = 1\n")]
    [InlineData("a..b = 1\n")]
    [InlineData("a. = 1\n")]
    public void Malformed_Dotted_Key_Is_Rejected(string text) 
        => Assert.False(Toml.TryParse(text).IsSuccess);

    [Theory]
    [InlineData("""
        [.]
        k = 1

        """)]
    [InlineData("""
        [..]
        k = 1

        """)]
    [InlineData("""
        [a.]

        """)]
    [InlineData("""
        [a..b]

        """)]
    [InlineData("""
        []

        """)]
    public void Malformed_Table_Path_Is_Rejected(string text) 
        => Assert.False(Toml.TryParse(text).IsSuccess);

    [Fact]
    public void Text_After_Table_Header_Is_Rejected()
    {
        var result = Toml.TryParse("[error] this should fail\n");

        Assert.False(result.IsSuccess);
        Assert.Equal(TomlDiagnosticCode.TrailingCharacters, Assert.Single(result.Diagnostics).Code); 
    }
}