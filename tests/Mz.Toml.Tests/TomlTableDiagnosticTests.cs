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
    [InlineData("[a.b.c]\nz = 9\n[a]\nb.c.t = \"invalid\"\n")]
    [InlineData("[a.b.c.d]\nz = 9\n[a]\nb.c.d.k.t = \"invalid\"\n")]
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
    [InlineData("name = \"Tom\"\nname = \"Pradyun\"\n")]
    [InlineData("spelling = \"favorite\"\n\"spelling\" = \"favourite\"\n")]
    [InlineData("spelling = \"favorite\"\n'spelling' = \"favourite\"\n")]
    [InlineData("\"\\u0061\" = 1\na = 2\n")]
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
    [InlineData("[.]\nk = 1\n")]
    [InlineData("[..]\nk = 1\n")]
    [InlineData("[a.]\n")]
    [InlineData("[a..b]\n")]
    [InlineData("[]\n")]
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
